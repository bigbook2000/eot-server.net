using cn.eobject.iot.Server.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WAIotServer.Logic;

namespace WAIotServer.Platform
{
    /// <summary>
    /// 权限处理
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class permitController : ControllerBase
    {
        [HttpPost]
        public JsonResult list(cls_result args)
        {
            cls_result cResult = CGlobal.IotDB.call_query_("eopx_permit_list", new()
            {
                { "v_permit_ids", "" }
            });

            return new JsonResult(cResult);
        }

        [HttpPost]
        public JsonResult upd(cls_result args)
        {
            cls_result_obj data = args.default_();
            cls_result cResult = CGlobal.IotDB.call_query_("eopx_permit_upd", new()
            {
                { "v_permit_id", data.to_int_("permit_id") },
                { "v_name", data.to_string_("name") },
                { "v_note", data.to_string_("note") }
            });

            return new JsonResult(cResult);
        }

        [HttpPost]
        public JsonResult del(cls_result args)
        {
            cls_result_obj data = args.default_();
            cls_result cResult = CGlobal.IotDB.call_query_("eopx_permit_del", new()
            {
                { "v_permit_id", data.to_int_("permit_id") },
            });

            return new JsonResult(cResult);
        }
    }
}
