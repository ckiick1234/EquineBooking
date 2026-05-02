using EquineBooking.Core.DTOs;
using EquineBooking.Core.Interfaces;
using EquineBooking.Core.Models;
using Microsoft.Extensions.Logging;

namespace EquineBooking.Infrastructure.Services;

/// <inheritdoc />
public sealed class AvailabilityService : IAvailabilityService
{
    private static readonly BookingStatus[] BlockingStatuses = { BookingStatus.Approved, BookingStatus.Pending };

    private readonly ISpaceRepository _spaceRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(
        ISpaceRepository spaceRepository,
        IBookingRepository bookingRepository,
        ILogger<AvailabilityService> logger)
    {
        _spaceRepository = spaceRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AvailabilityResponse>> GetAvailabilityAsync(
        string spaceId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("endDate must be on or after startDate.", nameof(endDate));
        }

        var space = await _spaceRepository.GetByIdAsync(spaceId, cancellationToken)
            ?? throw new InvalidOperationException($"Space '{spaceId}' not found.");

        var blockingBookings = (await _bookingRepository.GetBySpaceIdAsync(spaceId, cancellationToken))
            .Where(b => BlockingStatuses.Contains(b.Status))
            .ToList();

        _logger.LogDebug(
            "Computing availability for space {SpaceId} ({SlotType}) from {Start} to {End} against {Count} blocking bookings",
            spaceId, space.SlotType, startDate, endDate, blockingBookings.Count);

        var responses = new List<AvailabilityResponse>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var slots = space.SlotType switch
            {
                SlotType.Hourly => BuildHourlySlots(space, date, blockingBookings),
                SlotType.Daily => BuildDailySlot(date, blockingBookings),
                _ => Array.Empty<TimeSlot>()
            };
            responses.Add(new AvailabilityResponse(date, slots));
        }
        return responses;
    }

    public async Task<bool> IsSlotAvailableAsync(
        string spaceId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken = default)
    {
        if (end <= start)
        {
            return false;
        }

        var bookings = await _bookingRepository.GetBySpaceIdAsync(spaceId, cancellationToken);
        return !bookings.Any(b => BlockingStatuses.Contains(b.Status) && Overlaps(b.StartTime, b.EndTime, start, end));
    }

    private static IReadOnlyList<TimeSlot> BuildHourlySlots(Space space, DateOnly date, IReadOnlyList<Booking> bookings)
    {
        if (space.HourlyAvailability is null)
        {
            return Array.Empty<TimeSlot>();
        }

        var window = space.HourlyAvailability;
        var slots = new List<TimeSlot>();
        for (var hour = window.Start; hour < window.End; hour = hour.AddHours(1))
        {
            var slotStart = new DateTimeOffset(date.ToDateTime(hour), TimeSpan.Zero);
            var slotEnd = slotStart.AddHours(1);
            if (slotEnd > new DateTimeOffset(date.ToDateTime(window.End), TimeSpan.Zero))
            {
                break;
            }

            var available = !bookings.Any(b => Overlaps(b.StartTime, b.EndTime, slotStart, slotEnd));
            slots.Add(new TimeSlot(slotStart, slotEnd, available));
        }
        return slots;
    }

    private static IReadOnlyList<TimeSlot> BuildDailySlot(DateOnly date, IReadOnlyList<Booking> bookings)
    {
        var slotStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var slotEnd = slotStart.AddDays(1);
        var available = !bookings.Any(b => Overlaps(b.StartTime, b.EndTime, slotStart, slotEnd));
        return new[] { new TimeSlot(slotStart, slotEnd, available) };
    }

    private static bool Overlaps(DateTimeOffset aStart, DateTimeOffset aEnd, DateTimeOffset bStart, DateTimeOffset bEnd)
        => aStart < bEnd && bStart < aEnd;
}
