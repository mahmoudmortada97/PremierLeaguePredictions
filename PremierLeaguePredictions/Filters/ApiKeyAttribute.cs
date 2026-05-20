using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PremierLeaguePredictions.Filters
{
    public class ApiKeyAttribute : ActionFilterAttribute
    {
        private const string HeaderName = "X-Api-Key";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var key))
            {
                context.Result = new UnauthorizedObjectResult("API key is missing.");
                return;
            }

            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var validKey = config["ApiKey"];

            if (key != validKey)
                context.Result = new UnauthorizedObjectResult("Invalid API key.");
        }
    }
}