using EquineBooking.Api.Common;
using EquineBooking.Core.Interfaces;
using EquineBooking.Core.Models;
using EquineBooking.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace EquineBooking.Api.Functions;

/// <summary>Endpoints over the caller's own <see cref="UserProfile"/>.</summary>
public sealed class UserFunction
{
    private readonly IUserRepository _users;
    private readonly ILogger<UserFunction> _logger;

    public UserFunction(IUserRepository users, ILogger<UserFunction> logger)
    {
        _users = users;
        _logger = logger;
    }

    [Function("GetMe")]
    public async Task<IActionResult> GetMe(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/me")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var caller = req.HttpContext.GetUserContext();
        if (caller is null) return ApiResults.Unauthorized();

        var profile = await _users.GetByIdAsync(caller.ObjectId, cancellationToken);
        if (profile is not null) return new OkObjectResult(profile);

        // First sign-in: lazily provision a Client profile from JWT claims.
        var newProfile = new UserProfile
        {
            Id = caller.ObjectId,
            Email = caller.Email,
            DisplayName = caller.Name,
            Phone = string.Empty,
            Role = UserRole.Client,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };
        try
        {
            var created = await _users.CreateAsync(newProfile, cancellationToken);
            return new ObjectResult(created) { StatusCode = StatusCodes.Status201Created };
        }
        catch (ConflictException ex)
        {
            _logger.LogInformation(ex, "Race creating profile for {ObjectId} — re-reading", caller.ObjectId);
            var existing = await _users.GetByIdAsync(caller.ObjectId, cancellationToken);
            return existing is null ? ApiResults.Conflict(ex.Message) : new OkObjectResult(existing);
        }
    }

    [Function("UpdateMe")]
    public async Task<IActionResult> UpdateMe(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/me")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var caller = req.HttpContext.GetUserContext();
        if (caller is null) return ApiResults.Unauthorized();

        var existing = await _users.GetByIdAsync(caller.ObjectId, cancellationToken);
        if (existing is null) return ApiResults.NotFound("Profile not found.");

        UserProfile? body;
        try
        {
            body = await req.ReadFromJsonAsync<UserProfile>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Malformed UserProfile body");
            return ApiResults.BadRequest("Request body could not be parsed.");
        }
        if (body is null) return ApiResults.BadRequest("Request body is required.");

        // Only let the user mutate their own contact-detail fields; identity, role, and
        // active flag stay under admin control.
        existing.DisplayName = string.IsNullOrWhiteSpace(body.DisplayName) ? existing.DisplayName : body.DisplayName;
        existing.Phone = body.Phone ?? existing.Phone;

        try
        {
            var updated = await _users.UpdateAsync(existing, cancellationToken);
            return new OkObjectResult(updated);
        }
        catch (ConflictException ex)
        {
            return ApiResults.Conflict(ex.Message);
        }
    }
}
