using EquineBooking.Core.Interfaces;
using EquineBooking.Core.Models;
using Microsoft.Extensions.Logging;

namespace EquineBooking.Infrastructure.Services;

/// <summary>
/// <see cref="INotificationService"/> implementation for local development. Logs the
/// notification payload to the configured logger (which Functions Core Tools surfaces in
/// the console and Application Insights surfaces in the live metrics stream) instead of
/// dispatching real email. Registered when <c>ASPNETCORE_ENVIRONMENT</c> /
/// <c>DOTNET_ENVIRONMENT</c> is <c>Development</c>.
/// </summary>
public sealed class LocalDevNotificationService : INotificationService
{
    private readonly ILogger<LocalDevNotificationService> _logger;

    public LocalDevNotificationService(ILogger<LocalDevNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendBookingCreatedAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        Log("BookingCreated", booking, recipient: "admin",
            extra: $"Notes='{booking.Notes}'");
        return Task.CompletedTask;
    }

    public Task SendBookingApprovedAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        Log("BookingApproved", booking, recipient: booking.UserEmail);
        return Task.CompletedTask;
    }

    public Task SendBookingDeclinedAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        Log("BookingDeclined", booking, recipient: booking.UserEmail,
            extra: $"AdminNotes='{booking.AdminNotes}'");
        return Task.CompletedTask;
    }

    public Task SendBookingCancelledAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        Log("BookingCancelled", booking, recipient: "admin");
        return Task.CompletedTask;
    }

    public Task SendBookingModificationRequestedAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        Log("BookingModificationRequested", booking, recipient: "admin",
            extra: $"Notes='{booking.Notes}'");
        return Task.CompletedTask;
    }

    private void Log(string kind, Booking booking, string recipient, string? extra = null)
    {
        _logger.LogInformation(
            "[LOCAL EMAIL] kind={Kind} to={Recipient} booking={BookingId} space={SpaceId} user={UserName}<{UserEmail}> window={Start:o}->{End:o}{Extra}",
            kind,
            recipient,
            booking.Id,
            booking.SpaceId,
            booking.UserName,
            booking.UserEmail,
            booking.StartTime,
            booking.EndTime,
            extra is null ? string.Empty : " " + extra);
    }
}
