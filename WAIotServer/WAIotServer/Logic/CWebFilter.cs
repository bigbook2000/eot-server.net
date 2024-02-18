using cn.eobject.iot.Server.Log;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WAIotServer.Logic
{    
    public class CWebFilter : IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            string? ipStr = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            ipStr ??= "0.0.0.0";
            cls_log.get_default_().T_("", "<" + ipStr + ">" + context.HttpContext.Request.Path);
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            //cls_log.get_default_().T_("", context.HttpContext.Request.Path);
        }
    }
}
