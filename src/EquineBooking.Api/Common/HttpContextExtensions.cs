using EquineBooking.Api.Models;
using Microsoft.AspNetCore.Http;

namespace EquineBooking.Api.Common;

/// <summary>
/// Helpers for reading values stashed on <see cref="HttpContext"/> by the worker middleware.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>Returns the authenticated user context, or <c>null</c> for anonymous routes.</summary>
    public static UserContext? GetUserContext(this HttpContext context)
        => context.Items.TryGetValue(UserContext.HttpItemKey, out var value) ? value as UserContext : null;
}
