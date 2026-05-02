using System.Text.Json.Serialization;

namespace EquineBooking.Core.Models;

/// <summary>
/// Lifecycle state of a booking.
/// </summary>
public enum BookingStatus
{
    /// <summary>Submitted by a client and awaiting admin review.</summary>
    Pending,

    /// <summary>Admin has approved the booking.</summary>
    Approved,

    /// <summary>Admin has declined the booking.</summary>
    Declined,

    /// <summary>The booking was cancelled by the client or admin.</summary>
    Cancelled,

    /// <summary>Client has requested a change to an existing approved booking.</summary>
    ModificationRequested
}

/// <summary>
/// A reservation made by a client against a single <see cref="Space"/>.
/// Stored in Cosmos DB partitioned by <see cref="SpaceId"/>.
/// </summary>
public sealed class Booking
{
    /// <summary>Unique identifier for the booking.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Identifier of the <see cref="Space"/> being booked; also the Cosmos partition key.</summary>
    [JsonPropertyName("spaceId")]
    public string SpaceId { get; set; } = string.Empty;

    /// <summary>Identifier of the user who placed the booking.</summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>Email address of the booking user, denormalised for notifications.</summary>
    [JsonPropertyName("userEmail")]
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>Display name of the booking user, denormalised for admin views.</summary>
    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>Inclusive start of the reservation window.</summary>
    [JsonPropertyName("startTime")]
    public DateTimeOffset StartTime { get; set; }

    /// <summary>Exclusive end of the reservation window.</summary>
    [JsonPropertyName("endTime")]
    public DateTimeOffset EndTime { get; set; }

    /// <summary>Current lifecycle state.</summary>
    [JsonPropertyName("status")]
    public BookingStatus Status { get; set; }

    /// <summary>Free-form notes provided by the client (e.g. horse details, trailer info).</summary>
    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;

    /// <summary>Internal notes attached by an admin during review.</summary>
    [JsonPropertyName("adminNotes")]
    public string AdminNotes { get; set; } = string.Empty;

    /// <summary>UTC timestamp at which the booking was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC timestamp of the last update to the booking.</summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Whether a Venmo payment reminder should be sent to the client.</summary>
    [JsonPropertyName("venmoReminder")]
    public bool VenmoReminder { get; set; }

    /// <summary>
    /// Cosmos DB partition key. Mirrors <see cref="SpaceId"/> so all bookings for a
    /// given space share a logical partition.
    /// </summary>
    [JsonPropertyName("partitionKey")]
    public string PartitionKey => SpaceId;
}
