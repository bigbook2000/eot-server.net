using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using System.Security.Cryptography;
using System.Xml.Linq;
using WAIotServer.Common;
using WAIotServer.Logic;

namespace WAIotServer.Platfrom
{
    [Route("api/[controller]")]
    [ApiController]
    public class commonController : ControllerBase
    {
        private readonly IWebHostEnvironment mEnvironment;
        public commonController(IWebHostEnvironment env)
        {
            mEnvironment = env;
        }

        /// <summary>
        /// 转换参数
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private Dictionary<string, object> GetDBArgs(cls_result_obj data)
        {
            Dictionary<string, object> scriptArgs = new();

            cls_result_obj pms = data.to_json_("pms");

            foreach (KeyValuePair<string, object?> kvp in pms)
            {
                if (kvp.Value != null)
                    scriptArgs[kvp.Key] = kvp.Value;
            }

            return scriptArgs;
        }

        private bool CheckDBPermit(cls_result cResult, string scriptName)
        {
            if (!CGlobal.DBProcList.ContainsKey(scriptName))
            {
                cResult.set_error_("无对应接口");
                return false;
            }

            // 检查权限
            string permit = CGlobal.DBProcList[scriptName];
            if (permit.Length == 0)
            {
                // 可以随意调用
                return true;
            }

            eow_session_item? sessionItem = eow_session.handle_().get_(Request);
            if (sessionItem == null)
            {
                cResult.set_error_("验证失效");
                return false;
            }

            cls_result_obj? userData = (cls_result_obj?)sessionItem._user_data;
            if (userData == null)
            {
                cResult.set_error_("无用户信息");
                return false;
            }
            string permits = userData.to_string_("permits");

            if (!permits.Contains("," + permit + ","))
            {
                cResult.set_error_("无指定权限");
                return false;
            }

            return true;
        }

        [HttpPost]
        [Route("uuid")]
        public JsonResult uuid(cls_result args)
        {
            cls_result cResult = new();

            Guid guid = Guid.NewGuid();
            cResult.add_(new()
            {
                { "uuid", guid.ToString("N") }
            });

            return new JsonResult(cResult);
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("file/upload")]
        //[Consumes("application/x-www-form-urlencoded")]
        [Consumes("multipart/form-data")]
        public JsonResult fileUpload()
        {
            cls_result cResult = new();

            try
            {
                var form = Request.Form;

                string fileType = cls_core.o2str_(form["f_type"]);
                int fileKeyId = cls_core.o2int_(form["f_keyid"]);
                int fileIndex = cls_core.o2int_(form["f_index"]);
                string fileName = cls_core.o2str_(form["f_name"]);

                IFormFile file = form.Files[0];

                string[] fileInfo = CGlobal.GetFileInfo(file.FileName);
                string fileExt = fileInfo[2];
                if (fileExt.Length > 0) fileExt = "." + fileExt;

                // 计算md5
                MD5 md5 = MD5.Create();
                byte[] bytes;
                using (Stream fs = file.OpenReadStream())
                {
                    bytes = md5.ComputeHash(fs);
                    fs.Close();
                    fs.Dispose();
                }

                string md5Str = Convert.ToHexString(bytes).ToLower();
                string fileNew = "file/" + md5Str.Substring(0, 1) + "/" + md5Str + fileExt;

                string filePathNew = Path.Combine(mEnvironment.WebRootPath, fileNew);
                FileInfo fi = new(filePathNew);
                if (fi.Directory != null && !fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }

                int total = (int)file.Length;
                using (FileStream fs = new(filePathNew, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    file.CopyTo(fs);
                    fs.Flush();
                    fs.Close();
                    fs.Dispose();
                }

                // 存储到数据库中
                cls_result cQuery = new();
                CGlobal.DBScript.script_(cResult, "eopx_file_upd", new()
                {
                    { "v_type",  fileType },
                    { "v_keyid", fileKeyId },
                    { "v_index", fileIndex },
                    { "v_sign", md5Str },
                    { "v_name", fileName },
                    { "v_ext", fileExt },
                    { "v_total", total },
                    { "v_note", "" },
                });

                if (!cQuery.is_success_()) return new JsonResult(cQuery);

                cls_result_obj fileData = cQuery.default_();                
                fileData.Add("f_url", fileNew);

                cResult.set_success_();
            }
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }


        /// <summary>
        /// 文件信息
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("file/get")]
        public JsonResult fileGet(cls_result args)
        {
            cls_result cResult = new();

            try
            {
                cls_result_obj data = args.default_();
                string fileType = data.to_string_("f_type");
                int fileKeyId = data.to_int_("f_keyid");
                int fileIndex = data.to_int_("f_index");

                // 存储到数据库中
                CGlobal.DBScript.script_(cResult, "eopx_file_get", new()
                {
                    { "v_type",  fileType },
                    { "v_keyid", fileKeyId },
                    { "v_index", fileIndex }
                });
            }
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }


        /// <summary>
        /// 文件清单
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("file/list")]
        public JsonResult fileList(cls_result args)
        {
            cls_result cResult = new();

            try
            {
                cls_result_obj data = args.default_();
                string fileType = data.to_string_("f_type");
                string fileKeyIds = data.to_string_("f_keyids");

                // 存储到数据库中
                CGlobal.DBScript.script_(cResult, "eopx_file_list", new()
                {
                    { "v_type",  fileType },
                    { "v_keyids", fileKeyIds }
                });
            }
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }

        [HttpPost]
        [Route("proc")]
        public JsonResult proc(cls_result args)
        {
            cls_result cResult = new();
            
            try
            {
                cls_result_obj data = args.default_();

                string procName = data.to_string_("name");
                if (!CheckDBPermit(cResult, procName))
                {
                    return new JsonResult(cResult);
                }

                Dictionary<string, object> procArgs = GetDBArgs(data);

                cResult = CGlobal.DBScript.call_query_(procName, procArgs);
            }
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }

        [HttpPost]
        [Route("query")]
        public JsonResult query(cls_result args)
        {
            cls_result cResult = new();

            try
            {
                cls_result_obj data = args.default_();

                string scriptName = data.to_string_("name");
                if (!CheckDBPermit(cResult, scriptName))
                {
                    return new JsonResult(cResult);
                }

                Dictionary<string, object> scriptArgs = GetDBArgs(data);

                CGlobal.DBScript.script_(cResult, scriptName, scriptArgs);
            }
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }

        [HttpPost]
        [Route("setting/list")]
        public JsonResult settingList(cls_result args)
        {
            cls_result cResult = new();
            CGlobal.DBScript.script_(cResult, "eopx_setting_list", new());

            return new JsonResult(cResult);
        }
        [HttpPost]
        [Route("setting/upd")]
        public JsonResult settingUpd(cls_result args)
        {
            cls_result cResult = new();

            cls_result_obj data = args.default_();            
            CGlobal.DBScript.script_(cResult, "eopx_setting_upd", new()
            {
                { "v_key", data.to_string_("f_key") },
                { "v_value", data.to_string_("f_value") }
            });

            return new JsonResult(cResult);
        }

        /// <summary>
        /// 动态更新数据库接口
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("query/init")]
        public JsonResult queryInit(cls_result args)
        {
            CGlobal.DBScript.reload_();
            return new JsonResult(new cls_result());
        }
    }
}
