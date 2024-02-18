using cn.eobject.iot.Server.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using System.Text.Json;
using WAIotServer.Logic;

namespace WAIotServer.Controllers
{
    [Route("iot/gate")]
    [ApiController]
    public class IotGateController : ControllerBase
    {
        [Route("test")]
        [HttpPost]
        public JsonResult Test(cls_result data)
        {
            return new JsonResult(data);
        }

        [Route("config/get")]
        [HttpPost]
        public JsonResult ConfigGet(string dkey, string ver)
        {
            cls_result cResult = CGlobal.IotDB.call_query_("ep_config_get",
                new Dictionary<string, object>()
                {
                    { "v_device_key", dkey },
                    { "v_version_name", ver },
                });

            return new JsonResult(cResult);
        }

        [Route("config/set")]
        [HttpPost]
        public JsonResult ConfigSet(string dkey, string ver, string data)
        {
            cls_result cResult = CGlobal.IotDB.call_query_("ep_config_set",
                new Dictionary<string, object>()
                {
                    { "v_device_key", dkey },
                    { "v_version_code", ver },
                    { "v_config_data", data },
                });

            return new JsonResult(cResult);
        }
    }
}
