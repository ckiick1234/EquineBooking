using EquineBooking.Core.Interfaces;
using EquineBooking.Infrastructure.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EquineBooking.Api.Functions;

/// <summary>
/// Service Bus consumer that fans out booking lifecycle events to email/SMS via
/// <see cref="INotificationService"/>.
/// </summary>
public sealed class NotificationFunction
{
    private readonly INotificationService _notifications;
    private readonly IBookingRepository _bookings;
    private readonly ILogger<NotificationFunction> _logger;

    public NotificationFunction(
        INotificationService notifications,
        IBookingRepository bookings,
        ILogger<NotificationFunction> logger)
    {
        _notifications = notifications;
        _bookings = bookings;
        _logger = logger;
    }

    [Function("BookingNotificationProcessor")]
    public async Task Run(
        [ServiceBusTrigger("booking-notifications", Connection = "ServiceBus")] BookingNotification message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.BookingId))
        {
            _logger.LogWarning("Dropping malformed booking notification: bookingId missing");
            return;
        }

        var booking = await _bookings.FindByIdAsync(message.BookingId, cancellationToken);
        if (booking is null)
        {
            _logger.LogWarning("Booking {BookingId} not found — skipping {EventType}",
                message.BookingId, message.EventType);
            return;
        }

        _logger.LogInformation("Dispatching {EventType} notification for booking {BookingId} to {Recipient}",
            message.EventType, booking.Id, message.RecipientEmail);

        switch (message.EventType)
        {
            case BookingNotificationEventType.BookingCreated:
                await _notifications.SendBookingCreatedAsync(booking, cancellationToken);
                break;
            case BookingNotificationEventType.BookingApproved:
                await _notifications.SendBookingApprovedAsync(booking, cancellationToken);
                break;
            case BookingNotificationEventType.BookingDeclined:
                await _notifications.SendBookingDeclinedAsync(booking, cancellationToken);
                break;
            case BookingNotificationEventType.BookingCancelled:
                await _notifications.SendBookingCancelledAsync(booking, cancellationToken);
                break;
            case BookingNotificationEventType.BookingModificationRequested:
                await _notifications.SendBookingModificationRequestedAsync(booking, cancellationToken);
                break;
            default:
                _logger.LogWarning("Unknown booking notification event type {EventType}", message.EventType);
                break;
        }
    }
}
