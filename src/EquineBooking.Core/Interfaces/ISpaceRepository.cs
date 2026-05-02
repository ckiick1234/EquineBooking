using EquineBooking.Core.Models;

namespace EquineBooking.Core.Interfaces;

/// <summary>
/// Persistence operations for <see cref="Space"/> documents. Implementations
/// are expected to back this with Cosmos DB partitioned by <see cref="Space.Id"/>.
/// </summary>
public interface ISpaceRepository
{
    /// <summary>Fetch a single space by id.</summary>
    /// <param name="id">Space identifier (also the partition key).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The space, or <c>null</c> if not found.</returns>
    Task<Space?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Insert a new space.</summary>
    /// <param name="space">Space to persist. Must have <see cref="Space.Id"/> set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted space as stored.</returns>
    Task<Space> CreateAsync(Space space, CancellationToken cancellationToken = default);

    /// <summary>Replace an existing space.</summary>
    /// <param name="space">Space with updated state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted space as stored.</returns>
    Task<Space> UpdateAsync(Space space, CancellationToken cancellationToken = default);

    /// <summary>Delete a space by id.</summary>
    /// <param name="id">Space identifier (also the partition key).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>List all spaces with <see cref="Space.IsActive"/> set to true.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Space>> GetActiveAsync(CancellationToken cancellationToken = default);
}
