using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WAIotServer.Common;
using WAIotServer.Logic;

namespace WAIotServer.Platfrom
{
    [Route("api/[controller]")]
    [ApiController]
    public class commonController : ControllerBase
    {
        [HttpPost]
        [Route("uuid")]
        public JsonResult uuid(cls_result args)
        {
            cls_result cResult = new();

            Guid guid = Guid.NewGuid();
            cResult.add_(new()
            {
                { "uuid", guid.ToString("N") }
            });

            return new JsonResult(cResult);
        }

        [HttpPost]
        [Route("proc")]
        public JsonResult proc(cls_result args)
        {
            cls_result cResult = new();
            
            try
            {
                cls_result_obj data = args.default_();

                string procName = data.to_string_("name");
                if (!CGlobal.DBProcList.ContainsKey(procName))
                {
                    cResult.set_error_("无对应接口");
                    return new JsonResult(cResult);
                }
                string permit = CGlobal.DBProcList[procName];

                // 检查权限
                if (permit.Length > 0)
                {
                    eow_session_item? sessionItem = eow_session.handle_().get_(Request);
                    if (sessionItem == null)
                    {
                        cResult.set_error_("验证失效");
                        return new JsonResult(cResult);
                    }

                    cls_result_obj? userData = (cls_result_obj?) sessionItem._user_data;
                    if (userData == null)
                    {
                        cResult.set_error_("无用户信息");
                        return new JsonResult(cResult);
                    }
                    string permits = userData.to_string_("permits");

                    if (!permits.Contains("," + permit + ","))
                    {
                        cResult.set_error_("无指定权限");
                        return new JsonResult(cResult); 
                    }
                }

                cls_result_obj pms = data.to_json_("pms");

                Dictionary<string, object> procArgs = new();
                foreach (KeyValuePair<string, object?> kvp in pms)
                {
                    if (kvp.Value != null) 
                        procArgs[kvp.Key] = kvp.Value;
                }

                cResult = CGlobal.IotDB.call_query_(procName, procArgs);
            }
            catch (Exception ex)
            {
                cResult.set_except_(ex);
                cls_log.get_default_().T_("", ex.ToString());
            }

            return new JsonResult(cResult);
        }

        [HttpPost]
        [Route("setting/list")]
        public JsonResult settingList(cls_result args)
        {
            cls_result cResult = CGlobal.IotDB.call_query_("eopx_setting_list", new());

            return new JsonResult(cResult);
        }
        [HttpPost]
        [Route("setting/upd")]
        public JsonResult settingUpd(cls_result args)
        {
            cls_result_obj data = args.default_();
            cls_result cResult = CGlobal.IotDB.call_query_("eopx_setting_upd", new()
            {
                { "v_key", data.to_string_("key") },
                { "v_value", data.to_string_("value") }
            });

            return new JsonResult(cResult);
        }
    }
}
