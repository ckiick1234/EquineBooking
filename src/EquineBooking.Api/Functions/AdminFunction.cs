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

/// <summary>Admin-only endpoints: booking decisions and the user roster.</summary>
public sealed class AdminFunction
{
    private readonly IBookingRepository _bookings;
    private readonly ISpaceRepository _spaces;
    private readonly IUserRepository _users;
    private readonly AuthorizationHelper _auth;
    private readonly ILogger<AdminFunction> _logger;

    public AdminFunction(
        IBookingRepository bookings,
        ISpaceRepository spaces,
        IUserRepository users,
        AuthorizationHelper auth,
        ILogger<AdminFunction> logger)
    {
        _bookings = bookings;
        _spaces = spaces;
        _users = users;
        _auth = auth;
        _logger = logger;
    }

    [Function("DecideBooking")]
    public async Task<IActionResult> Decide(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "spaces/{spaceId}/bookings/{id}/decision")] HttpRequest req,
        string spaceId,
        string id,
        CancellationToken cancellationToken)
    {
        var caller = req.HttpContext.GetUserContext();
        if (caller is null) return ApiResults.Unauthorized();
        if (!await _auth.IsAdminAsync(caller, cancellationToken)) return ApiResults.Forbidden();

        AdminActionRequest? body;
        try
        {
            body = await req.ReadFromJsonAsync<AdminActionRequest>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Malformed AdminActionRequest body");
            return ApiResults.BadRequest("Request body could not be parsed.");
        }
        if (body is null) return ApiResults.BadRequest("Request body is required.");

        var booking = await _bookings.GetByIdAsync(id, spaceId, cancellationToken);
        if (booking is null) return ApiResults.NotFound($"Booking '{id}' not found.");

        booking.Status = body.Action switch
        {
            AdminAction.Approve => BookingStatus.Approved,
            AdminAction.Decline => BookingStatus.Declined,
            _ => booking.Status
        };
        if (body.AdminNotes is not null) booking.AdminNotes = body.AdminNotes;
        booking.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            var updated = await _bookings.UpdateAsync(booking, cancellationToken);
            var space = await _spaces.GetByIdAsync(spaceId, cancellationToken);
            return new OkObjectResult(BookingMapper.ToResponse(updated, space?.Name ?? string.Empty));
        }
        catch (ConflictException ex)
        {
            return ApiResults.Conflict(ex.Message);
        }
    }

    [Function("ListUsers")]
    public async Task<IActionResult> ListUsers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var caller = req.HttpContext.GetUserContext();
        if (caller is null) return ApiResults.Unauthorized();
        if (!await _auth.IsAdminAsync(caller, cancellationToken)) return ApiResults.Forbidden();

        var users = await _users.GetAllAsync(cancellationToken);
        return new OkObjectResult(users);
    }
}
