using System.Text.Json.Serialization;
using EquineBooking.Core.Models;

namespace EquineBooking.Core.DTOs;

/// <summary>
/// Booking projection returned to API clients. Mirrors <see cref="Booking"/> and
/// adds the resolved space name so callers do not need a second lookup.
/// </summary>
/// <param name="Id">Booking identifier.</param>
/// <param name="SpaceId">Identifier of the booked space.</param>
/// <param name="SpaceName">Display name of the booked space.</param>
/// <param name="UserId">Identifier of the booking user.</param>
/// <param name="UserEmail">Email of the booking user.</param>
/// <param name="UserName">Display name of the booking user.</param>
/// <param name="StartTime">Inclusive start of the reservation window.</param>
/// <param name="EndTime">Exclusive end of the reservation window.</param>
/// <param name="Status">Current lifecycle state.</param>
/// <param name="Notes">Client-supplied notes.</param>
/// <param name="AdminNotes">Admin-supplied notes.</param>
/// <param name="CreatedAt">Creation timestamp.</param>
/// <param name="UpdatedAt">Last-update timestamp.</param>
/// <param name="VenmoReminder">Whether a Venmo reminder is queued for this booking.</param>
public sealed record BookingResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("spaceId")] string SpaceId,
    [property: JsonPropertyName("spaceName")] string SpaceName,
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("userEmail")] string UserEmail,
    [property: JsonPropertyName("userName")] string UserName,
    [property: JsonPropertyName("startTime")] DateTimeOffset StartTime,
    [property: JsonPropertyName("endTime")] DateTimeOffset EndTime,
    [property: JsonPropertyName("status")] BookingStatus Status,
    [property: JsonPropertyName("notes")] string Notes,
    [property: JsonPropertyName("adminNotes")] string AdminNotes,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("updatedAt")] DateTimeOffset UpdatedAt,
    [property: JsonPropertyName("venmoReminder")] bool VenmoReminder
);
