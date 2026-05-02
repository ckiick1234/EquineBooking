namespace EquineBooking.Infrastructure.Data;

/// <summary>
/// Configuration values used to wire up the Cosmos DB client and context.
/// Bound from the <c>CosmosDb</c> configuration section.
/// </summary>
public sealed class CosmosOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "CosmosDb";

    /// <summary>
    /// Cosmos account endpoint for keyless (managed identity / DefaultAzureCredential) auth.
    /// Preferred in deployed environments. If both <see cref="Endpoint"/> and
    /// <see cref="ConnectionString"/> are set, <see cref="Endpoint"/> wins.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Cosmos DB account connection string. Used for the local emulator.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Database name housing the booking containers.</summary>
    public string DatabaseName { get; set; } = string.Empty;
}
