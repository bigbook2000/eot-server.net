using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Text.Json;
using WAIotServer.Logic;

namespace WAIotServer.Controllers
{
    [Route("iot/gate")]
    [ApiController]
    public class IotGateController : ControllerBase
    {
        private readonly IWebHostEnvironment mEnvironment;
        public IotGateController(IWebHostEnvironment env)
        {
            mEnvironment = env;
        }

        [Route("test")]
        [HttpGet, HttpPost]
        public async Task<JsonResult> Test(string info)
        {
            await Task.Delay(3000);

            return new JsonResult(info);
        }

        /// <summary>
        /// 直接从设备获取配置信息
        /// HJ212自定义命令
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [Route("iot/command")]
        [HttpPost]
        public async Task<JsonResult> IotCommand(cls_result args)
        {
            cls_result cResult = new();

            cls_result_obj data = args.default_();
            string sMN = data.to_string_("mn");
            string sST = data.to_string_("st");
            string sCN = data.to_string_("cn");
            string sCP = data.to_string_("cp");

            string? sRet = await CGlobal.IotServer.SendCommand(sMN, sST, sCN, sCP);
            if (sRet == null)
            {
                cResult.set_error_("设备未响应");
                return new JsonResult(cResult);
            }

            cResult.add_(new()
            {
                { "mn", sMN },
                { "st", sST },
                { "cn", sCN },
                { "cp", sRet }
            });
            cResult.set_success_();

            return new JsonResult(cResult);
        }
    }
}
