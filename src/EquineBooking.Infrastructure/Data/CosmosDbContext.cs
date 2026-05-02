using Microsoft.Azure.Cosmos;

namespace EquineBooking.Infrastructure.Data;

/// <summary>
/// Resolves the Cosmos DB <see cref="Container"/> handles used by the repositories.
/// Containers are loaded eagerly at construction so callers do not pay the lookup cost per call.
/// </summary>
public sealed class CosmosDbContext
{
    /// <summary>Container holding <see cref="EquineBooking.Core.Models.Booking"/> documents, partitioned by <c>/spaceId</c>.</summary>
    public Container Bookings { get; }

    /// <summary>Container holding <see cref="EquineBooking.Core.Models.Space"/> documents, partitioned by <c>/id</c>.</summary>
    public Container Spaces { get; }

    /// <summary>Container holding <see cref="EquineBooking.Core.Models.UserProfile"/> documents, partitioned by <c>/id</c>.</summary>
    public Container Users { get; }

    public CosmosDbContext(CosmosClient client, string databaseName)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var database = client.GetDatabase(databaseName);
        Bookings = database.GetContainer("bookings");
        Spaces = database.GetContainer("spaces");
        Users = database.GetContainer("users");
    }
}
