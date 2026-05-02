using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace EquineBooking.Api.Middleware;

/// <summary>
/// Lightweight CORS middleware that opens up <c>http://localhost:4200</c> for the local
/// Angular dev server. Production CORS is expected to be configured at the Function App
/// host level.
/// </summary>
public sealed class CorsMiddleware : IFunctionsWorkerMiddleware
{
    private const string AllowedOrigin = "http://localhost:4200";

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var http = context.GetHttpContext();
        if (http is null)
        {
            await next(context);
            return;
        }

        http.Response.Headers["Access-Control-Allow-Origin"] = AllowedOrigin;
        http.Response.Headers["Vary"] = "Origin";
        http.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
        http.Response.Headers["Access-Control-Allow-Headers"] = "Authorization, Content-Type";
        http.Response.Headers["Access-Control-Allow-Credentials"] = "true";

        if (HttpMethods.IsOptions(http.Request.Method))
        {
            http.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        await next(context);
    }
}
