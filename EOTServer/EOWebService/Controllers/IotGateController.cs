using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.X86;
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

            try
            {
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
            }
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }

        /// <summary>
        /// 版本升级
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [Route("iot/version/update")]
        [HttpPost]
        public async Task<JsonResult> IotVersionUpdate(cls_result args)
        {
            cls_result cResult = new();

            try
            {
                cls_result_obj data = args.default_();
                cResult.add_(data);

                // 设备标识
                string sMN = data.to_string_("mn");
                string sType = data.to_string_("type");
                string sVersion = data.to_string_("version");
                int nTotal = data.to_int_("total");
                string sCrc = data.to_string_("sign");
                string sUrl = data.to_string_("url");

                // 先给设备发送3109升级命令
                string? sRet = await CGlobal.IotServer.SendVersionUpdate(
                    sMN, sType, sVersion, nTotal, sCrc, sUrl);
                if (sRet != null)
                {
                    cResult.set_error_(sRet);
                    return new JsonResult(cResult);
                }
            }
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }
    }
}
