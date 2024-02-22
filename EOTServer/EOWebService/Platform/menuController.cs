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
            cls_result cResult = CGlobal.IotDB.call_query_("eopx_menu_list", new());

            return new JsonResult(cResult);
        }

        [HttpPost]
        public JsonResult upd(cls_result args)
        {
            cls_result_obj data = args.default_();
            cls_result cResult = CGlobal.IotDB.call_query_("eopx_menu_upd", new ()
            {
                { "v_menu_id", data.to_int_("menu_id") },
                { "v_menu_pid", data.to_int_("menu_pid") },
                { "v_order", data.to_int_("order") },
                { "v_level", data.to_int_("level") },
                { "v_name", data.to_string_("name") },
                { "v_type", data.to_string_("type") },
                { "v_icon", data.to_string_("icon") },
                { "v_path", data.to_string_("path") },
                { "v_permit", data.to_string_("permit") },
                { "v_status", data.to_int_("status") },
            });

            return new JsonResult(cResult);
        }

        [HttpPost]
        public JsonResult del(cls_result args)
        {
            cls_result_obj data = args.default_();
            cls_result cResult = CGlobal.IotDB.call_query_("eopx_menu_del", new()
            {
                { "v_menu_id", data.to_int_("menu_id") },
            });

            return new JsonResult(cResult);
        }
    }
}
