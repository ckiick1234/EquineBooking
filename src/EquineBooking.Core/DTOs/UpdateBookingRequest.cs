using System.Text.Json.Serialization;

namespace EquineBooking.Core.DTOs;

/// <summary>
/// Partial update to an existing booking. Any field left null is treated as unchanged.
/// </summary>
/// <param name="StartTime">New inclusive start of the reservation window, if changing.</param>
/// <param name="EndTime">New exclusive end of the reservation window, if changing.</param>
/// <param name="Notes">Replacement client notes, if changing.</param>
public sealed record UpdateBookingRequest(
    [property: JsonPropertyName("startTime")] DateTimeOffset? StartTime,
    [property: JsonPropertyName("endTime")] DateTimeOffset? EndTime,
    [property: JsonPropertyName("notes")] string? Notes
);
