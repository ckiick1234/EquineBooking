using System.Text.Json.Serialization;

namespace EquineBooking.Core.DTOs;

/// <summary>
/// Decision an admin can apply to a pending booking.
/// </summary>
public enum AdminAction
{
    /// <summary>Approve the booking.</summary>
    Approve,

    /// <summary>Decline the booking.</summary>
    Decline
}

/// <summary>
/// Request submitted by an admin when reviewing a booking.
/// </summary>
/// <param name="Action">The decision being applied.</param>
/// <param name="AdminNotes">Optional notes recorded with the decision.</param>
public sealed record AdminActionRequest(
    [property: JsonPropertyName("action")] AdminAction Action,
    [property: JsonPropertyName("adminNotes")] string? AdminNotes
);
