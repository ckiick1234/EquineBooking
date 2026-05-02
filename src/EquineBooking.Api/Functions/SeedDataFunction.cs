using EquineBooking.Api.Common;
using EquineBooking.Core.Interfaces;
using EquineBooking.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EquineBooking.Api.Functions;

/// <summary>
/// Seeds reference data (spaces, dev users) the first time the app runs against a fresh
/// Cosmos database. Idempotent: each item is only created if it doesn't already exist.
///
/// Access:
///  - In Development the endpoint is open (typical local dev flow against the emulator).
///  - In any other environment the caller must be an authenticated admin.
/// </summary>
public sealed class SeedDataFunction
{
    public const string CorralId = "round-corral";
    public const string StallId = "covered-stall";
    public const string AdminUserId = "dev-admin";
    public const string ClientUserId = "dev-client";

    private readonly ISpaceRepository _spaces;
    private readonly IUserRepository _users;
    private readonly AuthorizationHelper _auth;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SeedDataFunction> _logger;

    public SeedDataFunction(
        ISpaceRepository spaces,
        IUserRepository users,
        AuthorizationHelper auth,
        IHostEnvironment environment,
        ILogger<SeedDataFunction> logger)
    {
        _spaces = spaces;
        _users = users;
        _auth = auth;
        _environment = environment;
        _logger = logger;
    }

    [Function("SeedData")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "seed")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            var caller = req.HttpContext.GetUserContext();
            if (caller is null) return ApiResults.Unauthorized();
            if (!await _auth.IsAdminAsync(caller, cancellationToken)) return ApiResults.Forbidden();
        }

        var summary = new SeedSummary();

        await SeedSpaceAsync(BuildCorral(), summary, cancellationToken);
        await SeedSpaceAsync(BuildStall(), summary, cancellationToken);
        await SeedUserAsync(BuildAdmin(), summary, cancellationToken);
        await SeedUserAsync(BuildClient(), summary, cancellationToken);

        _logger.LogInformation(
            "Seed complete. Spaces created={SpacesCreated} skipped={SpacesSkipped}, Users created={UsersCreated} skipped={UsersSkipped}",
            summary.SpacesCreated, summary.SpacesSkipped, summary.UsersCreated, summary.UsersSkipped);

        return new OkObjectResult(summary);
    }

    private async Task SeedSpaceAsync(Space space, SeedSummary summary, CancellationToken ct)
    {
        var existing = await _spaces.GetByIdAsync(space.Id, ct);
        if (existing is not null)
        {
            summary.SpacesSkipped++;
            return;
        }
        await _spaces.CreateAsync(space, ct);
        summary.SpacesCreated++;
    }

    private async Task SeedUserAsync(UserProfile user, SeedSummary summary, CancellationToken ct)
    {
        var existing = await _users.GetByIdAsync(user.Id, ct);
        if (existing is not null)
        {
            summary.UsersSkipped++;
            return;
        }
        await _users.CreateAsync(user, ct);
        summary.UsersCreated++;
    }

    private static Space BuildCorral() => new()
    {
        Id = CorralId,
        Name = "Round Corral",
        Type = SpaceType.Corral,
        SlotType = SlotType.Hourly,
        Description = "Outdoor round corral suitable for groundwork and lunging.",
        IsActive = true,
        Rules = "Clean up after your horse. No grain feeding inside the corral.",
        HourlyAvailability = new HourlyAvailability
        {
            Start = new TimeOnly(7, 0),
            End = new TimeOnly(19, 0)
        }
    };

    private static Space BuildStall() => new()
    {
        Id = StallId,
        Name = "Covered Stall",
        Type = SpaceType.Stall,
        SlotType = SlotType.Daily,
        Description = "Covered stall with auto-waterer. Booked by full day.",
        IsActive = true,
        Rules = "Provide your own bedding. Strip stall on departure.",
        HourlyAvailability = null
    };

    private static UserProfile BuildAdmin() => new()
    {
        Id = AdminUserId,
        Email = "admin@equine.local",
        DisplayName = "Dev Admin",
        Phone = "+15555550100",
        Role = UserRole.Admin,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static UserProfile BuildClient() => new()
    {
        Id = ClientUserId,
        Email = "client@equine.local",
        DisplayName = "Dev Client",
        Phone = "+15555550101",
        Role = UserRole.Client,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow
    };

    /// <summary>Result of the seed run.</summary>
    public sealed class SeedSummary
    {
        public int SpacesCreated { get; set; }
        public int SpacesSkipped { get; set; }
        public int UsersCreated { get; set; }
        public int UsersSkipped { get; set; }
    }
}
