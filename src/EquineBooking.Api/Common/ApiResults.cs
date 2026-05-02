using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EquineBooking.Api.Common;

/// <summary>
/// Convenience factory for <see cref="ProblemDetails"/> payloads with appropriate status codes.
/// Centralises the error shape so all functions return the same body for the same class of failure.
/// </summary>
public static class ApiResults
{
    public static IActionResult Problem(int status, string title, string? detail = null)
        => new ObjectResult(new ProblemDetails { Status = status, Title = title, Detail = detail })
        {
            StatusCode = status
        };

    public static IActionResult BadRequest(string detail) => Problem(StatusCodes.Status400BadRequest, "Bad Request", detail);
    public static IActionResult Unauthorized() => Problem(StatusCodes.Status401Unauthorized, "Unauthorized");
    public static IActionResult Forbidden() => Problem(StatusCodes.Status403Forbidden, "Forbidden");
    public static IActionResult NotFound(string detail) => Problem(StatusCodes.Status404NotFound, "Not Found", detail);
    public static IActionResult Conflict(string detail) => Problem(StatusCodes.Status409Conflict, "Conflict", detail);
}
