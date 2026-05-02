using EquineBooking.Core.Models;

namespace EquineBooking.Core.Interfaces;

/// <summary>
/// Persistence operations for <see cref="UserProfile"/> documents. Implementations
/// are expected to back this with Cosmos DB partitioned by <see cref="UserProfile.Id"/>.
/// </summary>
public interface IUserRepository
{
    /// <summary>Fetch a single user profile by id.</summary>
    /// <param name="id">User identifier (also the partition key).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile, or <c>null</c> if not found.</returns>
    Task<UserProfile?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Insert a new user profile.</summary>
    /// <param name="user">Profile to persist. Must have <see cref="UserProfile.Id"/> set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted profile as stored.</returns>
    Task<UserProfile> CreateAsync(UserProfile user, CancellationToken cancellationToken = default);

    /// <summary>Replace an existing user profile.</summary>
    /// <param name="user">Profile with updated state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted profile as stored.</returns>
    Task<UserProfile> UpdateAsync(UserProfile user, CancellationToken cancellationToken = default);

    /// <summary>Delete a user profile by id.</summary>
    /// <param name="id">User identifier (also the partition key).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Fetch a single user profile by email address.</summary>
    /// <param name="email">Email address to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile, or <c>null</c> if no match exists.</returns>
    Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>List every user profile in the system. Intended for admin tooling only.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<UserProfile>> GetAllAsync(CancellationToken cancellationToken = default);
}
