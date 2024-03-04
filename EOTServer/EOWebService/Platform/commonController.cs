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

                string fileType = cls_core.o2str_(form["type"]);
                int fileKeyId = cls_core.o2int_(form["keyid"]);
                int fileIndex = cls_core.o2int_(form["index"]);
                string fileName = cls_core.o2str_(form["name"]);

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
                FileInfo fi = new FileInfo(filePathNew);
                if (fi.Directory != null && !fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }

                using (FileStream fs = new FileStream(filePathNew, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    file.CopyTo(fs);
                    fs.Flush();
                    fs.Close();
                    fs.Dispose();
                }

                // 存储到数据库中
                cls_result cQuery = CGlobal.IotDB.call_query_("eopx_file_upd", new()
                {
                    { "v_type",  fileType },
                    { "v_keyid", fileKeyId },
                    { "v_index", fileIndex },
                    { "v_sign", md5Str },
                    { "v_name", fileName },
                    { "v_ext", fileExt },
                    { "v_note", "" },
                });

                if (!cQuery.is_success_()) return new JsonResult(cQuery);

                int fileId = cQuery.default_().to_int_("file_id");

                cls_result_obj fileData = new();
                cResult.add_(fileData);

                fileData.Add("type", fileType);
                fileData.Add("keyid", fileKeyId);
                fileData.Add("index", fileIndex);                
                fileData.Add("sign", md5Str);
                fileData.Add("name", fileName);
                fileData.Add("ext", fileExt);
                
                fileData.Add("url", fileNew);
                fileData.Add("file_id", fileId);

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
                string fileType = data.to_string_("type");
                int fileKeyId = data.to_int_("keyid");
                int fileIndex = data.to_int_("index");

                // 存储到数据库中
                cResult = CGlobal.IotDB.call_query_("eopx_file_get", new()
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
                string fileType = data.to_string_("type");
                string fileKeyIds = data.to_string_("keyids");

                // 存储到数据库中
                cResult = CGlobal.IotDB.call_query_("eopx_file_list", new()
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
                if (!CGlobal.DBProcList.ContainsKey(procName))
                {
                    cResult.set_error_("无对应接口");
                    return new JsonResult(cResult);
                }
                string permit = CGlobal.DBProcList[procName];

                // 检查权限
                if (permit.Length > 0)
                {
                    eow_session_item? sessionItem = eow_session.handle_().get_(Request);
                    if (sessionItem == null)
                    {
                        cResult.set_error_("验证失效");
                        return new JsonResult(cResult);
                    }

                    cls_result_obj? userData = (cls_result_obj?) sessionItem._user_data;
                    if (userData == null)
                    {
                        cResult.set_error_("无用户信息");
                        return new JsonResult(cResult);
                    }
                    string permits = userData.to_string_("permits");

                    if (!permits.Contains("," + permit + ","))
                    {
                        cResult.set_error_("无指定权限");
                        return new JsonResult(cResult); 
                    }
                }

                cls_result_obj pms = data.to_json_("pms");

                Dictionary<string, object> procArgs = new();
                foreach (KeyValuePair<string, object?> kvp in pms)
                {
                    if (kvp.Value != null) 
                        procArgs[kvp.Key] = kvp.Value;
                }

                cResult = CGlobal.IotDB.call_query_(procName, procArgs);
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
            cls_result cResult = CGlobal.IotDB.call_query_("eopx_setting_list", new());

            return new JsonResult(cResult);
        }
        [HttpPost]
        [Route("setting/upd")]
        public JsonResult settingUpd(cls_result args)
        {
            cls_result_obj data = args.default_();
            cls_result cResult = CGlobal.IotDB.call_query_("eopx_setting_upd", new()
            {
                { "v_key", data.to_string_("key") },
                { "v_value", data.to_string_("value") }
            });

            return new JsonResult(cResult);
        }
    }
}
