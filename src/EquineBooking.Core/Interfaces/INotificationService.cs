using EquineBooking.Core.Models;

namespace EquineBooking.Core.Interfaces;

/// <summary>
/// Sends out-of-band notifications (email, SMS, etc.) for booking lifecycle events.
/// </summary>
public interface INotificationService
{
    /// <summary>Notify the admin and the client that a new booking has been created.</summary>
    /// <param name="booking">The newly created booking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendBookingCreatedAsync(Booking booking, CancellationToken cancellationToken = default);

    /// <summary>Notify the client that their booking has been approved.</summary>
    /// <param name="booking">The approved booking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendBookingApprovedAsync(Booking booking, CancellationToken cancellationToken = default);

    /// <summary>Notify the client that their booking has been declined.</summary>
    /// <param name="booking">The declined booking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendBookingDeclinedAsync(Booking booking, CancellationToken cancellationToken = default);

    /// <summary>Notify the admin and the client that a booking has been cancelled.</summary>
    /// <param name="booking">The cancelled booking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendBookingCancelledAsync(Booking booking, CancellationToken cancellationToken = default);

    /// <summary>Notify the admin that a client has requested a modification to a booking.</summary>
    /// <param name="booking">The booking with a pending modification request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendBookingModificationRequestedAsync(Booking booking, CancellationToken cancellationToken = default);
}
