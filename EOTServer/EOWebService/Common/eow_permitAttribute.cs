using cn.eobject.iot.Server.Core;
using cn.eobject.iot.Server.Log;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace WAIotServer.Common
{
    public class eow_permitAttribute : Attribute, IActionFilter
    {
        public string Value { get; set; } = "";

        public eow_permitAttribute() 
        { 
        }
        public eow_permitAttribute(string value)
        {
            Value = value;
        }

        private JsonResult set_result_(string msg)
        {
            cls_result cResult = new cls_result();
            cResult.set_error_(msg);
            return new JsonResult(cResult);
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // 赋值即中断
            //context.Result = set_result_("测试");

            string? ipStr = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            ipStr ??= "-";

            HttpRequest httpRequest = context.HttpContext.Request;
            StringValues svs = httpRequest.Headers["Authorization"];
            if (svs.Count <= 0 || svs[0] == null)
            {
                cls_log.get_default_().T_("", "异常调用<" + ipStr + ">" + httpRequest.Path);
                context.Result = set_result_("会话失败1");
                return;
            }
            string? token = svs[0];
            token ??= "";
            eow_session_item? sessionItem = eow_session.handle_().get_(token);
            if (sessionItem == null)
            {
                cls_log.get_default_().T_("", "会话超时<" + ipStr + ">" + httpRequest.Path);
                context.Result = set_result_("会话失败2");
                return;
            }            

            if (Value.Length > 0 && sessionItem._user_data != null)
            {
                // 检查权限，登录时设定
                cls_result_obj userData = (cls_result_obj) sessionItem._user_data;
                string permits = userData.to_string_("permits");

                if (!permits.Contains("," + Value + ","))
                {
                    cls_log.get_default_().T_("", "无指定权限<" + ipStr + ">[" + Value + "]: " + httpRequest.Path);
                    context.Result = set_result_("无指定权限");
                    return;
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
