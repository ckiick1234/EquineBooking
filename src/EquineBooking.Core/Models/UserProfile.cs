using System.Text.Json.Serialization;

namespace EquineBooking.Core.Models;

/// <summary>
/// Authorisation role for a <see cref="UserProfile"/>.
/// </summary>
public enum UserRole
{
    /// <summary>Facility administrator; can review, approve, and decline bookings.</summary>
    Admin,

    /// <summary>End user who places bookings.</summary>
    Client
}

/// <summary>
/// Profile data for a user of the booking system.
/// Stored in Cosmos DB partitioned by <see cref="Id"/>.
/// </summary>
public sealed class UserProfile
{
    /// <summary>Unique identifier for the user; also the Cosmos partition key.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Login email address. Unique across the system.</summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Friendly name shown in the UI and on bookings.</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Contact phone number in E.164 format when available.</summary>
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>Authorisation role assigned to this user.</summary>
    [JsonPropertyName("role")]
    public UserRole Role { get; set; }

    /// <summary>UTC timestamp at which the profile was created.</summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Whether the user is currently allowed to sign in and make bookings.</summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    /// <summary>
    /// Cosmos DB partition key. Mirrors <see cref="Id"/> so each user lives in its
    /// own logical partition.
    /// </summary>
    [JsonPropertyName("partitionKey")]
    public string PartitionKey => Id;
}
