using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Aiursoft.WebTools.Attributes;

public class EnforceWebSocket : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        if (!context.HttpContext.WebSockets.IsWebSocketRequest)
        {
            context.Result = new StatusCodeResult(400);
        }
    }
}