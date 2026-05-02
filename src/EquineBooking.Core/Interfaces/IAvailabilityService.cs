using EquineBooking.Core.DTOs;

namespace EquineBooking.Core.Interfaces;

/// <summary>
/// Computes which slots of a space are open for booking, taking the space's
/// configuration and existing bookings into account.
/// </summary>
public interface IAvailabilityService
{
    /// <summary>
    /// Compute per-day availability for a space across the given date range.
    /// </summary>
    /// <param name="spaceId">Space identifier.</param>
    /// <param name="startDate">Inclusive first date to evaluate.</param>
    /// <param name="endDate">Inclusive last date to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One <see cref="AvailabilityResponse"/> per date in the range, in chronological order.</returns>
    Task<IReadOnlyList<AvailabilityResponse>> GetAvailabilityAsync(
        string spaceId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check whether a specific time window for a space is currently bookable.
    /// </summary>
    /// <param name="spaceId">Space identifier.</param>
    /// <param name="start">Inclusive start of the proposed window.</param>
    /// <param name="end">Exclusive end of the proposed window.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> when the slot is free; <c>false</c> when blocked or out of policy.</returns>
    Task<bool> IsSlotAvailableAsync(
        string spaceId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken = default);
}
