using System.Text.Json.Serialization;

namespace EquineBooking.Core.DTOs;

/// <summary>
/// One candidate slot within an availability response.
/// </summary>
/// <param name="Start">Inclusive start of the slot.</param>
/// <param name="End">Exclusive end of the slot.</param>
/// <param name="IsAvailable">True if the slot can currently be booked.</param>
public sealed record TimeSlot(
    [property: JsonPropertyName("start")] DateTimeOffset Start,
    [property: JsonPropertyName("end")] DateTimeOffset End,
    [property: JsonPropertyName("isAvailable")] bool IsAvailable
);

/// <summary>
/// Per-day availability for a single space.
/// </summary>
/// <param name="Date">Calendar date the slots belong to.</param>
/// <param name="AvailableSlots">All slots offered on <paramref name="Date"/>, in chronological order.</param>
public sealed record AvailabilityResponse(
    [property: JsonPropertyName("date")] DateOnly Date,
    [property: JsonPropertyName("availableSlots")] IReadOnlyList<TimeSlot> AvailableSlots
);
