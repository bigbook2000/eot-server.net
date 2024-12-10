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
            cls_result cResult = new();
            CGlobal.DBScript.script_(cResult, "eopx_permit_list", new()
            {
                { "v_permit_ids", "" }
            });

            return new JsonResult(cResult);
        }

        [HttpPost]
        public JsonResult upd(cls_result args)
        {
            cls_result cResult = new();
            
            cls_result_obj data = args.default_();
            CGlobal.DBScript.script_(cResult, "eopx_permit_upd", new()
            {
                { "v_permit_id", data.to_int_("f_permit_id") },
                { "v_name", data.to_string_("f_name") },
                { "v_note", data.to_string_("f_note") }
            });

            return new JsonResult(cResult);
        }

        [HttpPost]
        public JsonResult del(cls_result args)
        {
            cls_result cResult = new();
            
            cls_result_obj data = args.default_();
            CGlobal.DBScript.script_(cResult, "eopx_permit_del", new()
            {
                { "v_permit_id", data.to_int_("f_permit_id") },
            });

            return new JsonResult(cResult);
        }
    }
}
