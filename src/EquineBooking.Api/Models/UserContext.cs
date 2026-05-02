namespace EquineBooking.Api.Models;

/// <summary>
/// Identity for the caller of an HTTP-triggered function, populated by
/// <see cref="EquineBooking.Api.Middleware.AuthMiddleware"/> from the validated JWT.
/// </summary>
/// <param name="ObjectId">Entra External ID <c>oid</c> claim — stable per user.</param>
/// <param name="Email">Primary email from the <c>emails</c> claim.</param>
/// <param name="Name">Display name from the <c>name</c> claim.</param>
public sealed record UserContext(string ObjectId, string Email, string Name)
{
    /// <summary>Key used to stash the context on <c>HttpContext.Items</c>.</summary>
    public const string HttpItemKey = "EquineBooking.UserContext";
}
