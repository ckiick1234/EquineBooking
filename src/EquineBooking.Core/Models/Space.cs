using System.Text.Json.Serialization;

namespace EquineBooking.Core.Models;

/// <summary>
/// Physical category of a bookable space.
/// </summary>
public enum SpaceType
{
    /// <summary>An outdoor corral, typically booked in hourly slots.</summary>
    Corral,

    /// <summary>An indoor stall, typically booked by the day.</summary>
    Stall
}

/// <summary>
/// Granularity at which a space can be reserved.
/// </summary>
public enum SlotType
{
    /// <summary>Reservations are made in hour-sized blocks within a daily window.</summary>
    Hourly,

    /// <summary>Reservations occupy whole calendar days.</summary>
    Daily
}

/// <summary>
/// Daily window during which a corral may be booked, expressed as wall-clock times.
/// </summary>
public sealed class HourlyAvailability
{
    /// <summary>Earliest start time of day (inclusive) at which an hourly slot may begin.</summary>
    [JsonPropertyName("start")]
    public TimeOnly Start { get; set; }

    /// <summary>Latest end time of day (exclusive) by which an hourly slot must finish.</summary>
    [JsonPropertyName("end")]
    public TimeOnly End { get; set; }
}

/// <summary>
/// A bookable space at the equine facility (corral or stall).
/// Stored in Cosmos DB partitioned by <see cref="Id"/>.
/// </summary>
public sealed class Space
{
    /// <summary>Unique identifier for the space; also serves as the Cosmos partition key.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name shown to clients (e.g. "North Corral").</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Physical category of the space.</summary>
    [JsonPropertyName("type")]
    public SpaceType Type { get; set; }

    /// <summary>Booking granularity supported by this space.</summary>
    [JsonPropertyName("slotType")]
    public SlotType SlotType { get; set; }

    /// <summary>Long-form description of the space.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Whether the space is currently published and accepting bookings.</summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    /// <summary>
    /// Daily window for hourly bookings. Required when <see cref="SlotType"/> is
    /// <see cref="SlotType.Hourly"/>; ignored otherwise.
    /// </summary>
    [JsonPropertyName("hourlyAvailability")]
    public HourlyAvailability? HourlyAvailability { get; set; }

    /// <summary>House rules and usage notes shown to the client at booking time.</summary>
    [JsonPropertyName("rules")]
    public string Rules { get; set; } = string.Empty;

    /// <summary>
    /// Cosmos DB partition key. Mirrors <see cref="Id"/> so each space lives in its own
    /// logical partition.
    /// </summary>
    [JsonPropertyName("partitionKey")]
    public string PartitionKey => Id;
}
