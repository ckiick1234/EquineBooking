using System.Net;
using EquineBooking.Core.Interfaces;
using EquineBooking.Core.Models;
using EquineBooking.Infrastructure.Data;
using EquineBooking.Infrastructure.Exceptions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace EquineBooking.Infrastructure.Repositories;

/// <inheritdoc />
public sealed class SpaceRepository : ISpaceRepository
{
    private readonly Container _container;
    private readonly ILogger<SpaceRepository> _logger;

    public SpaceRepository(CosmosDbContext context, ILogger<SpaceRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        _container = context.Spaces;
        _logger = logger;
    }

    public async Task<Space?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<Space>(id, new PartitionKey(id), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Space {SpaceId} not found", id);
            return null;
        }
    }

    public async Task<Space> CreateAsync(Space space, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(space);
        try
        {
            var response = await _container.CreateItemAsync(space, new PartitionKey(space.Id), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogWarning(ex, "Conflict creating space {SpaceId}", space.Id);
            throw new ConflictException($"Space '{space.Id}' already exists.", ex);
        }
    }

    public async Task<Space> UpdateAsync(Space space, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(space);
        try
        {
            var response = await _container.ReplaceItemAsync(space, space.Id, new PartitionKey(space.Id), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict || ex.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            _logger.LogWarning(ex, "Conflict updating space {SpaceId}", space.Id);
            throw new ConflictException($"Space '{space.Id}' was modified concurrently.", ex);
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.DeleteItemAsync<Space>(id, new PartitionKey(id), cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Space {SpaceId} already absent", id);
        }
    }

    public async Task<IReadOnlyList<Space>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.isActive = @isActive")
            .WithParameter("@isActive", true);

        var results = new List<Space>();
        using var iterator = _container.GetItemQueryIterator<Space>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page);
        }
        return results;
    }
}
