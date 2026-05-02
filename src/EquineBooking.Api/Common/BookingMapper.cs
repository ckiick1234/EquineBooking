using EquineBooking.Core.DTOs;
using EquineBooking.Core.Models;

namespace EquineBooking.Api.Common;

/// <summary>
/// Projects <see cref="Booking"/> documents onto <see cref="BookingResponse"/> wire DTOs.
/// </summary>
public static class BookingMapper
{
    public static BookingResponse ToResponse(Booking booking, string spaceName) => new(
        booking.Id,
        booking.SpaceId,
        spaceName,
        booking.UserId,
        booking.UserEmail,
        booking.UserName,
        booking.StartTime,
        booking.EndTime,
        booking.Status,
        booking.Notes,
        booking.AdminNotes,
        booking.CreatedAt,
        booking.UpdatedAt,
        booking.VenmoReminder);
}
