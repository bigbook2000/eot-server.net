using cn.eobject.iot.Server.Core;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using WAIotServer.Common;
using WAIotServer.Logic;

namespace WAIotServer.Platform
{
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
            cls_result_obj data = args.default_();
            cls_result cResult = CGlobal.IotDB.call_query_("eopx_dic_list", new()
            {
                { "v_dic_pid", data.to_int_("dic_pid") },
            });

            return new JsonResult(cResult);
        }
        [HttpPost]
        public JsonResult upd(cls_result args)
        {
            cls_result_obj data = args.default_();
            cls_result cResult = CGlobal.IotDB.call_query_("eopx_dic_upd", new()
            {
                { "v_dic_id", data.to_int_("dic_id") },
                { "v_dic_pid", data.to_int_("dic_pid") },
                { "v_level", data.to_int_("level") },
                { "v_label", data.to_string_("label") },
                { "v_value", data.to_string_("value") },
                { "v_note", data.to_string_("note") },
            });

            return new JsonResult(cResult);
        }
        [HttpPost]
        public JsonResult del(cls_result args)
        {
            cls_result_obj data = args.default_();
            cls_result cResult = CGlobal.IotDB.call_query_("eopx_dic_del", new()
            {
                { "v_dic_id", data.to_int_("dic_id") },
            });

            return new JsonResult(cResult);
        }
        [HttpPost]
        public JsonResult create(cls_result args)
        {
            cls_result cResult = new ();
            cls_result cQuery;

            // 创建一个字典json文件
            cQuery = CGlobal.IotDB.call_value_("eopx_dic_version", new());
            if (!cQuery.is_success_())
            {
                return new JsonResult(cResult);
            }
            int nVersion = cls_core.o2int_(cQuery.get_scalar());

            cQuery = CGlobal.IotDB.call_query_("eopx_dic_list", new()
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
