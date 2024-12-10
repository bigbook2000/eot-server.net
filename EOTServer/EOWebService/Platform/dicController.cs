using cn.eobject.iot.Server.Core;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using WAIotServer.Common;
using WAIotServer.Logic;

namespace WAIotServer.Platform
{
    /// <summary>
    /// 浏览其使用json文件缓存
    /// 减少服务端传输和数据库压力
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class dicController : ControllerBase
    {
        private readonly IWebHostEnvironment mEnvironment;

        public dicController(IWebHostEnvironment env)
        {
            mEnvironment = env;
        }

        [HttpPost]
        [eow_permit]
        public JsonResult list(cls_result args)
        {
            cls_result cResult = new();

            cls_result_obj data = args.default_();
            CGlobal.DBScript.script_(cResult, "eopx_dic_list", new()
            {
                { "v_dic_pid", data.to_int_("f_dic_pid") },
            });

            return new JsonResult(cResult);
        }
        [HttpPost]
        public JsonResult upd(cls_result args)
        {
            cls_result cResult = new();

            cls_result_obj data = args.default_();
             CGlobal.DBScript.script_(cResult, "eopx_dic_upd", new()
            {
                { "v_dic_id", data.to_int_("f_dic_id") },
                { "v_dic_pid", data.to_int_("f_dic_pid") },
                { "v_level", data.to_int_("f_level") },
                { "v_label", data.to_string_("f_label") },
                { "v_value", data.to_string_("f_value") },
                { "v_note", data.to_string_("f_note") },
            });

            return new JsonResult(cResult);
        }
        [HttpPost]
        public JsonResult del(cls_result args)
        {
            cls_result cResult = new();

            cls_result_obj data = args.default_();
            CGlobal.DBScript.script_(cResult, "eopx_dic_del", new()
            {
                { "v_dic_id", data.to_int_("f_dic_id") },
            });

            return new JsonResult(cResult);
        }
        /// <summary>
        /// 使用json文件，通过浏览器缓存，减少服务端传输和数据库压力
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult create(cls_result args)
        {
            cls_result cResult = new();

            cls_result cQuery;

            // 创建一个字典json文件
            cQuery = new();
            CGlobal.DBScript.script_(cQuery, "eopx_dic_version", new());
            if (!cQuery.is_success_())
            {
                return new JsonResult(cResult);
            }
            int nVersion = cls_core.o2int_(cQuery.get_scalar());

            cQuery = new();
            CGlobal.DBScript.script_(cQuery, "eopx_dic_list", new()
            {
                { "v_dic_pid", -1 },
            });
            if (!cQuery.is_success_())
            {
                return new JsonResult(cResult);
            }

            StringBuilder sb = new ();
            bool bFirst = true;

            string sFile = Path.Combine(
                mEnvironment.WebRootPath, "static", "dic" + nVersion + ".json");
            using (StreamWriter sw = new(sFile, false, Encoding.UTF8))
            {
                sw.Write("[");
                foreach (cls_result_obj obj in cQuery._list)
                {
                    sb.Clear();

                    if (bFirst)
                        sb.Append('{');
                    else
                        sb.Append(',').Append('{');
                    bFirst = false;

                    foreach (var item in obj)
                    {
                        sb.Append('"').Append(item.Key).Append('"').Append(':')
                            .Append('"').Append(item.Value).Append('"').Append(',');
                    }
                    sb.Remove(sb.Length - 1, 1);
                    sb.Append('}');

                    sw.Write(sb);
                }
                sw.Write("]");

                sw.Close();
                sw.Dispose();
            }

            cResult.set_success_();

            return new JsonResult(cResult);
        }

    }
}
