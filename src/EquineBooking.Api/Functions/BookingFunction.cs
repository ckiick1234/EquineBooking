using EquineBooking.Api.Common;
using EquineBooking.Core.DTOs;
using EquineBooking.Core.Interfaces;
using EquineBooking.Core.Models;
using EquineBooking.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace EquineBooking.Api.Functions;

/// <summary>CRUD endpoints over <see cref="Booking"/>.</summary>
public sealed class BookingFunction
{
    private readonly IBookingRepository _bookings;
    private readonly ISpaceRepository _spaces;
    private readonly IAvailabilityService _availability;
    private readonly AuthorizationHelper _auth;
    private readonly ILogger<BookingFunction> _logger;

    public BookingFunction(
        IBookingRepository bookings,
        ISpaceRepository spaces,
        IAvailabilityService availability,
        AuthorizationHelper auth,
        ILogger<BookingFunction> logger)
    {
        _bookings = bookings;
        _spaces = spaces;
        _availability = availability;
        _auth = auth;
        _logger = logger;
    }

    [Function("ListBookings")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bookings")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var caller = req.HttpContext.GetUserContext();
        if (caller is null) return ApiResults.Unauthorized();

        var isAdmin = await _auth.IsAdminAsync(caller, cancellationToken);
        IReadOnlyList<Booking> bookings;

        if (isAdmin && Enum.TryParse<BookingStatus>(req.Query["status"], ignoreCase: true, out var statusFilter))
        {
            bookings = await _bookings.GetByStatusAsync(statusFilter, cancellationToken);
        }
        else if (isAdmin)
        {
            // Admin without a status filter: return pending review queue by default.
            bookings = await _bookings.GetByStatusAsync(BookingStatus.Pending, cancellationToken);
        }
        else
        {
            bookings = await _bookings.GetByUserIdAsync(caller.ObjectId, cancellationToken);
        }

        var responses = new List<BookingResponse>(bookings.Count);
        var spaceNameCache = new Dictionary<string, string>();
        foreach (var booking in bookings)
        {
            if (!spaceNameCache.TryGetValue(booking.SpaceId, out var name))
            {
                var space = await _spaces.GetByIdAsync(booking.SpaceId, cancellationToken);
                name = space?.Name ?? string.Empty;
                spaceNameCache[booking.SpaceId] = name;
            }
            responses.Add(BookingMapper.ToResponse(booking, name));
        }
        return new OkObjectResult(responses);
    }

    [Function("GetBooking")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "spaces/{spaceId}/bookings/{id}")] HttpRequest req,
        string spaceId,
        string id,
        CancellationToken cancellationToken)
    {
        var caller = req.HttpContext.GetUserContext();
        if (caller is null) return ApiResults.Unauthorized();

        var booking = await _bookings.GetByIdAsync(id, spaceId, cancellationToken);
        if (booking is null) return ApiResults.NotFound($"Booking '{id}' not found.");

        var isAdmin = await _auth.IsAdminAsync(caller, cancellationToken);
        if (!isAdmin && !string.Equals(booking.UserId, caller.ObjectId, StringComparison.Ordinal))
        {
            return ApiResults.Forbidden();
        }

        var space = await _spaces.GetByIdAsync(spaceId, cancellationToken);
        return new OkObjectResult(BookingMapper.ToResponse(booking, space?.Name ?? string.Empty));
    }

    [Function("CreateBooking")]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bookings")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var caller = req.HttpContext.GetUserContext();
        if (caller is null) return ApiResults.Unauthorized();

        CreateBookingRequest? body;
        try
        {
            body = await req.ReadFromJsonAsync<CreateBookingRequest>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Malformed CreateBookingRequest body");
            return ApiResults.BadRequest("Request body could not be parsed.");
        }
        if (body is null) return ApiResults.BadRequest("Request body is required.");
        if (string.IsNullOrWhiteSpace(body.SpaceId)) return ApiResults.BadRequest("spaceId is required.");
        if (body.EndTime <= body.StartTime) return ApiResults.BadRequest("endTime must be after startTime.");

        var space = await _spaces.GetByIdAsync(body.SpaceId, cancellationToken);
        if (space is null || !space.IsActive) return ApiResults.NotFound($"Space '{body.SpaceId}' not found.");

        if (!await _availability.IsSlotAvailableAsync(body.SpaceId, body.StartTime, body.EndTime, cancellationToken))
        {
            return ApiResults.Conflict("Requested time window is not available.");
        }

        var now = DateTimeOffset.UtcNow;
        var booking = new Booking
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = body.SpaceId,
            UserId = caller.ObjectId,
            UserEmail = caller.Email,
            UserName = caller.Name,
            StartTime = body.StartTime,
            EndTime = body.EndTime,
            Status = BookingStatus.Pending,
            Notes = body.Notes ?? string.Empty,
            AdminNotes = string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
            VenmoReminder = false
        };

        try
        {
            var created = await _bookings.CreateAsync(booking, cancellationToken);
            return new ObjectResult(BookingMapper.ToResponse(created, space.Name)) { StatusCode = StatusCodes.Status201Created };
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "Conflict creating booking for space {SpaceId}", body.SpaceId);
            return ApiResults.Conflict(ex.Message);
        }
    }

    [Function("UpdateBooking")]
    public async Task<IActionResult> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "spaces/{spaceId}/bookings/{id}")] HttpRequest req,
        string spaceId,
        string id,
        CancellationToken cancellationToken)
    {
        var caller = req.HttpContext.GetUserContext();
        if (caller is null) return ApiResults.Unauthorized();

        var existing = await _bookings.GetByIdAsync(id, spaceId, cancellationToken);
        if (existing is null) return ApiResults.NotFound($"Booking '{id}' not found.");

        var isAdmin = await _auth.IsAdminAsync(caller, cancellationToken);
        if (!isAdmin && !string.Equals(existing.UserId, caller.ObjectId, StringComparison.Ordinal))
        {
            return ApiResults.Forbidden();
        }

        UpdateBookingRequest? body;
        try
        {
            body = await req.ReadFromJsonAsync<UpdateBookingRequest>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Malformed UpdateBookingRequest body");
            return ApiResults.BadRequest("Request body could not be parsed.");
        }
        if (body is null) return ApiResults.BadRequest("Request body is required.");

        var newStart = body.StartTime ?? existing.StartTime;
        var newEnd = body.EndTime ?? existing.EndTime;
        if (newEnd <= newStart) return ApiResults.BadRequest("endTime must be after startTime.");

        existing.StartTime = newStart;
        existing.EndTime = newEnd;
        if (body.Notes is not null) existing.Notes = body.Notes;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        // Clients editing an approved booking flag it for re-review.
        if (!isAdmin && existing.Status == BookingStatus.Approved)
        {
            existing.Status = BookingStatus.ModificationRequested;
        }

        try
        {
            var updated = await _bookings.UpdateAsync(existing, cancellationToken);
            var space = await _spaces.GetByIdAsync(spaceId, cancellationToken);
            return new OkObjectResult(BookingMapper.ToResponse(updated, space?.Name ?? string.Empty));
        }
        catch (ConflictException ex)
        {
            return ApiResults.Conflict(ex.Message);
        }
    }

    [Function("DeleteBooking")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "spaces/{spaceId}/bookings/{id}")] HttpRequest req,
        string spaceId,
        string id,
        CancellationToken cancellationToken)
    {
        var caller = req.HttpContext.GetUserContext();
        if (caller is null) return ApiResults.Unauthorized();

        var existing = await _bookings.GetByIdAsync(id, spaceId, cancellationToken);
        if (existing is null) return ApiResults.NotFound($"Booking '{id}' not found.");

        var isAdmin = await _auth.IsAdminAsync(caller, cancellationToken);
        if (!isAdmin && !string.Equals(existing.UserId, caller.ObjectId, StringComparison.Ordinal))
        {
            return ApiResults.Forbidden();
        }

        existing.Status = BookingStatus.Cancelled;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _bookings.UpdateAsync(existing, cancellationToken);
            return new NoContentResult();
        }
        catch (ConflictException ex)
        {
            return ApiResults.Conflict(ex.Message);
        }
    }
}
