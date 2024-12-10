using cn.eobject.iot.Server.Core;
using Microsoft.AspNetCore.Mvc;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using WAIotServer.Common;
using WAIotServer.Logic;

namespace WAIotServer.Platfrom
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class menuController
    {
        [HttpPost]
        public JsonResult list(cls_result args)
        {
            cls_result cResult = new();
            CGlobal.DBScript.script_(cResult, "eopx_menu_list", new());

            return new JsonResult(cResult);
        }

        [HttpPost]
        public JsonResult upd(cls_result args)
        {
            cls_result cResult = new();

            cls_result_obj data = args.default_();
            CGlobal.DBScript.script_(cResult, "eopx_menu_upd", new ()
            {
                { "v_menu_id", data.to_int_("f_menu_id") },
                { "v_menu_pid", data.to_int_("f_menu_pid") },
                { "v_order", data.to_int_("f_order") },
                { "v_level", data.to_int_("f_level") },
                { "v_name", data.to_string_("f_name") },
                { "v_type", data.to_string_("f_type") },
                { "v_icon", data.to_string_("f_icon") },
                { "v_path", data.to_string_("f_path") },
                { "v_permit", data.to_string_("f_permit") },
                { "v_status", data.to_int_("f_status") },
            });

            return new JsonResult(cResult);
        }

        [HttpPost]
        public JsonResult del(cls_result args)
        {
            cls_result cResult = new();

            cls_result_obj data = args.default_();
            CGlobal.DBScript.script_(cResult, "eopx_menu_del", new()
            {
                { "v_menu_id", data.to_int_("f_menu_id") },
            });

            return new JsonResult(cResult);
        }
    }
}
