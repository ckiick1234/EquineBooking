using System.Text.Json.Serialization;

namespace EquineBooking.Infrastructure.Models;

/// <summary>
/// Lifecycle event signalled to the notification queue.
/// </summary>
public enum BookingNotificationEventType
{
    /// <summary>A new booking has been created — admin needs to review.</summary>
    BookingCreated,

    /// <summary>An existing booking has been approved — notify the client.</summary>
    BookingApproved,

    /// <summary>An existing booking has been declined — notify the client.</summary>
    BookingDeclined,

    /// <summary>An existing booking has been cancelled — notify the admin.</summary>
    BookingCancelled,

    /// <summary>The client has requested a modification — admin needs to re-review.</summary>
    BookingModificationRequested
}

/// <summary>
/// Message body delivered on the <c>booking-notifications</c> Service Bus queue.
/// </summary>
/// <param name="BookingId">Identifier of the booking the event relates to.</param>
/// <param name="EventType">Lifecycle event being notified.</param>
/// <param name="RecipientEmail">Pre-resolved email of the party being notified (admin or client).</param>
/// <param name="RecipientName">Display name of the recipient, used in the email greeting.</param>
public sealed record BookingNotification(
    [property: JsonPropertyName("bookingId")] string BookingId,
    [property: JsonPropertyName("eventType")] BookingNotificationEventType EventType,
    [property: JsonPropertyName("recipientEmail")] string RecipientEmail,
    [property: JsonPropertyName("recipientName")] string RecipientName
);
