using System.Text.Json.Serialization;

namespace EquineBooking.Core.DTOs;

/// <summary>
/// Payload submitted by a client to create a new booking.
/// </summary>
/// <param name="SpaceId">Identifier of the space being booked.</param>
/// <param name="StartTime">Inclusive start of the requested reservation window.</param>
/// <param name="EndTime">Exclusive end of the requested reservation window.</param>
/// <param name="Notes">Optional free-form notes from the client.</param>
public sealed record CreateBookingRequest(
    [property: JsonPropertyName("spaceId")] string SpaceId,
    [property: JsonPropertyName("startTime")] DateTimeOffset StartTime,
    [property: JsonPropertyName("endTime")] DateTimeOffset EndTime,
    [property: JsonPropertyName("notes")] string? Notes
);
