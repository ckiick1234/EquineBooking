using System.Net;
using EquineBooking.Core.Interfaces;
using EquineBooking.Core.Models;
using EquineBooking.Infrastructure.Data;
using EquineBooking.Infrastructure.Exceptions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace EquineBooking.Infrastructure.Repositories;

/// <inheritdoc />
public sealed class UserRepository : IUserRepository
{
    private readonly Container _container;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(CosmosDbContext context, ILogger<UserRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        _container = context.Users;
        _logger = logger;
    }

    public async Task<UserProfile?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<UserProfile>(id, new PartitionKey(id), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug("User {UserId} not found", id);
            return null;
        }
    }

    public async Task<UserProfile> CreateAsync(UserProfile user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        try
        {
            var response = await _container.CreateItemAsync(user, new PartitionKey(user.Id), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogWarning(ex, "Conflict creating user {UserId}", user.Id);
            throw new ConflictException($"User '{user.Id}' already exists.", ex);
        }
    }

    public async Task<UserProfile> UpdateAsync(UserProfile user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        try
        {
            var response = await _container.ReplaceItemAsync(user, user.Id, new PartitionKey(user.Id), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict || ex.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            _logger.LogWarning(ex, "Conflict updating user {UserId}", user.Id);
            throw new ConflictException($"User '{user.Id}' was modified concurrently.", ex);
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.DeleteItemAsync<UserProfile>(id, new PartitionKey(id), cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug("User {UserId} already absent", id);
        }
    }

    public async Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.email = @email")
            .WithParameter("@email", email);

        using var iterator = _container.GetItemQueryIterator<UserProfile>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            foreach (var user in page)
            {
                return user;
            }
        }
        return null;
    }

    public async Task<IReadOnlyList<UserProfile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c");
        var results = new List<UserProfile>();
        using var iterator = _container.GetItemQueryIterator<UserProfile>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page);
        }
        return results;
    }
}
