using EquineBooking.Core.Models;

namespace EquineBooking.Core.Interfaces;

/// <summary>
/// Persistence operations for <see cref="Booking"/> documents. Implementations
/// are expected to back this with Cosmos DB partitioned by <see cref="Booking.SpaceId"/>.
/// </summary>
public interface IBookingRepository
{
    /// <summary>Fetch a single booking by id and partition.</summary>
    /// <param name="id">Booking identifier.</param>
    /// <param name="spaceId">Partition key (space id).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The booking, or <c>null</c> if not found.</returns>
    Task<Booking?> GetByIdAsync(string id, string spaceId, CancellationToken cancellationToken = default);

    /// <summary>Insert a new booking.</summary>
    /// <param name="booking">Booking to persist. Must have <see cref="Booking.Id"/> and <see cref="Booking.SpaceId"/> set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted booking as stored.</returns>
    Task<Booking> CreateAsync(Booking booking, CancellationToken cancellationToken = default);

    /// <summary>Replace an existing booking.</summary>
    /// <param name="booking">Booking with updated state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted booking as stored.</returns>
    Task<Booking> UpdateAsync(Booking booking, CancellationToken cancellationToken = default);

    /// <summary>Delete a booking by id and partition.</summary>
    /// <param name="id">Booking identifier.</param>
    /// <param name="spaceId">Partition key (space id).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(string id, string spaceId, CancellationToken cancellationToken = default);

    /// <summary>List all bookings placed by the given user.</summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Booking>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>List all bookings against the given space.</summary>
    /// <param name="spaceId">Space identifier (partition key).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Booking>> GetBySpaceIdAsync(string spaceId, CancellationToken cancellationToken = default);

    /// <summary>List all bookings currently in the given lifecycle state.</summary>
    /// <param name="status">Status to filter on.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Booking>> GetByStatusAsync(BookingStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// List all bookings whose reservation window overlaps the given range.
    /// </summary>
    /// <param name="start">Inclusive range start.</param>
    /// <param name="end">Exclusive range end.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Booking>> GetByDateRangeAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find a booking by id when the partition key isn't known. Issues a cross-partition
    /// query — prefer <see cref="GetByIdAsync"/> when the space id is available.
    /// </summary>
    /// <param name="id">Booking identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The booking, or <c>null</c> if not found.</returns>
    Task<Booking?> FindByIdAsync(string id, CancellationToken cancellationToken = default);
}
