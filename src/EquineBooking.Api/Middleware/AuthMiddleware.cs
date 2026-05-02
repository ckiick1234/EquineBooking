using EquineBooking.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace EquineBooking.Api.Middleware;

/// <summary>
/// Worker-pipeline middleware that validates the bearer token via the registered JwtBearer
/// scheme and projects the resulting principal onto a <see cref="UserContext"/>.
///
/// The Entra External ID JWT bearer handler is configured in <c>Program.cs</c> via
/// <c>AddMicrosoftIdentityWebApi("AzureAd")</c>; this middleware just drives it and
/// stashes the result on <c>HttpContext.Items</c> for downstream functions.
///
/// Anonymous routes (currently <c>/api/health</c> and <c>/api/seed</c>) bypass auth entirely.
/// </summary>
public sealed class AuthMiddleware : IFunctionsWorkerMiddleware
{
    private static readonly string[] AnonymousPathPrefixes = { "/api/health", "/api/seed" };

    private readonly ILogger<AuthMiddleware> _logger;

    public AuthMiddleware(ILogger<AuthMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var http = context.GetHttpContext();
        if (http is null)
        {
            // Non-HTTP trigger (e.g. Service Bus) — auth doesn't apply.
            await next(context);
            return;
        }

        if (IsAnonymous(http.Request.Path))
        {
            await next(context);
            return;
        }

        var result = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal is null)
        {
            _logger.LogInformation("Rejecting unauthenticated request to {Path}", http.Request.Path);
            http.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await http.Response.WriteAsync("Unauthorized");
            return;
        }

        var principal = result.Principal;
        var oid = principal.FindFirst("oid")?.Value
                  ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                  ?? principal.FindFirst("sub")?.Value;
        var email = principal.FindFirst("emails")?.Value
                    ?? principal.FindFirst("email")?.Value
                    ?? principal.FindFirst("preferred_username")?.Value
                    ?? string.Empty;
        var name = principal.FindFirst("name")?.Value ?? string.Empty;

        if (string.IsNullOrEmpty(oid))
        {
            _logger.LogWarning("JWT missing object identifier claim on path {Path}", http.Request.Path);
            http.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await http.Response.WriteAsync("Unauthorized");
            return;
        }

        http.User = principal;
        http.Items[UserContext.HttpItemKey] = new UserContext(oid, email, name);
        await next(context);
    }

    private static bool IsAnonymous(PathString path)
    {
        foreach (var prefix in AnonymousPathPrefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
