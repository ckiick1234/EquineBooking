using System.Net;
using EquineBooking.Core.Interfaces;
using EquineBooking.Core.Models;
using EquineBooking.Infrastructure.Data;
using EquineBooking.Infrastructure.Exceptions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace EquineBooking.Infrastructure.Repositories;

/// <inheritdoc />
public sealed class BookingRepository : IBookingRepository
{
    private readonly Container _container;
    private readonly ILogger<BookingRepository> _logger;

    public BookingRepository(CosmosDbContext context, ILogger<BookingRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        _container = context.Bookings;
        _logger = logger;
    }

    public async Task<Booking?> GetByIdAsync(string id, string spaceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<Booking>(id, new PartitionKey(spaceId), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Booking {BookingId} not found in space {SpaceId}", id, spaceId);
            return null;
        }
    }

    public async Task<Booking> CreateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(booking);
        try
        {
            var response = await _container.CreateItemAsync(booking, new PartitionKey(booking.SpaceId), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogWarning(ex, "Conflict creating booking {BookingId} in space {SpaceId}", booking.Id, booking.SpaceId);
            throw new ConflictException($"Booking '{booking.Id}' already exists in space '{booking.SpaceId}'.", ex);
        }
    }

    public async Task<Booking> UpdateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(booking);
        try
        {
            var response = await _container.ReplaceItemAsync(booking, booking.Id, new PartitionKey(booking.SpaceId), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict || ex.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            _logger.LogWarning(ex, "Conflict updating booking {BookingId} in space {SpaceId}", booking.Id, booking.SpaceId);
            throw new ConflictException($"Booking '{booking.Id}' was modified concurrently.", ex);
        }
    }

    public async Task DeleteAsync(string id, string spaceId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.DeleteItemAsync<Booking>(id, new PartitionKey(spaceId), cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Booking {BookingId} in space {SpaceId} already absent", id, spaceId);
        }
    }

    public Task<IReadOnlyList<Booking>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
            .WithParameter("@userId", userId);
        return ExecuteQueryAsync(query, crossPartition: true, cancellationToken);
    }

    public Task<IReadOnlyList<Booking>> GetBySpaceIdAsync(string spaceId, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.spaceId = @spaceId")
            .WithParameter("@spaceId", spaceId);
        var requestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(spaceId) };
        return ExecuteQueryAsync(query, requestOptions, cancellationToken);
    }

    public Task<IReadOnlyList<Booking>> GetByStatusAsync(BookingStatus status, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.status = @status")
            .WithParameter("@status", status.ToString());
        return ExecuteQueryAsync(query, crossPartition: true, cancellationToken);
    }

    public Task<IReadOnlyList<Booking>> GetByDateRangeAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.startTime >= @start AND c.startTime < @end")
            .WithParameter("@start", start)
            .WithParameter("@end", end);
        return ExecuteQueryAsync(query, crossPartition: true, cancellationToken);
    }

    public async Task<Booking?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
            .WithParameter("@id", id);
        using var iterator = _container.GetItemQueryIterator<Booking>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            foreach (var booking in page)
            {
                return booking;
            }
        }
        return null;
    }

    private Task<IReadOnlyList<Booking>> ExecuteQueryAsync(QueryDefinition query, bool crossPartition, CancellationToken cancellationToken)
    {
        var options = new QueryRequestOptions();
        if (crossPartition)
        {
            // No partition key set => cross-partition scan.
        }
        return ExecuteQueryAsync(query, options, cancellationToken);
    }

    private async Task<IReadOnlyList<Booking>> ExecuteQueryAsync(QueryDefinition query, QueryRequestOptions options, CancellationToken cancellationToken)
    {
        var results = new List<Booking>();
        using var iterator = _container.GetItemQueryIterator<Booking>(query, requestOptions: options);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page);
        }
        return results;
    }
}
