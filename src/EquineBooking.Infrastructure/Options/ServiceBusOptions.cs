namespace EquineBooking.Infrastructure.Options;

/// <summary>
/// Bound from the <c>ServiceBus</c> configuration section.
/// </summary>
public sealed class ServiceBusOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "ServiceBus";

    /// <summary>
    /// Fully-qualified namespace (e.g. <c>my-ns.servicebus.windows.net</c>) for keyless auth via
    /// <c>DefaultAzureCredential</c>. Preferred in deployed environments. If both this and
    /// <see cref="ConnectionString"/> are set, this wins.
    /// </summary>
    public string FullyQualifiedNamespace { get; set; } = string.Empty;

    /// <summary>Connection string for the Service Bus namespace hosting the notifications queue.</summary>
    public string ConnectionString { get; set; } = string.Empty;
}
