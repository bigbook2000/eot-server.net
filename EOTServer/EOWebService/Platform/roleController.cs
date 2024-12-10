using cn.eobject.iot.Server.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.NetworkInformation;
using WAIotServer.Logic;

namespace WAIotServer.Platform
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class roleController : ControllerBase
    {
        [HttpPost]
        public JsonResult list(cls_result args)
        {
            cls_result cResult = new();
            CGlobal.DBScript.script_(cResult, "eopx_role_list", new());
            return new JsonResult(cResult);
        }

        [HttpPost]
        public JsonResult upd(cls_result args)
        {
            cls_result cResult = new();

            cls_result_obj data = args.default_();
            CGlobal.DBScript.script_(cResult, "eopx_role_upd", new()
            {
                { "v_role_id", data.to_int_("f_role_id") },
                { "v_name", data.to_string_("f_name") },
                { "v_note", data.to_string_("f_note") },
                { "v_status", data.to_int_("f_status") },
            });
            return new JsonResult(cResult);
        }

        [HttpPost]
        public JsonResult del(cls_result args)
        {
            cls_result cResult = new();

            cls_result_obj data = args.default_();
            CGlobal.DBScript.script_(cResult, "eopx_role_del", new()
            {
                { "v_role_id", data.to_int_("f_role_id") },
            });

            return new JsonResult(cResult);
        }

        [HttpPost]
        public JsonResult permits(cls_result args)
        {
            cls_result cResult = new();

            cls_result_obj data = args.default_();
            CGlobal.DBScript.script_(cResult, "eopx_role_permits", new()
            {
                { "v_role_id", data.to_int_("f_role_id") },
                { "v_permits", data.to_string_("f_permits") },
            });

            return new JsonResult(cResult);
        }
    }
}
