using EquineBooking.Api.Common;
using EquineBooking.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace EquineBooking.Api.Functions;

/// <summary>Availability lookups for a single space.</summary>
public sealed class AvailabilityFunction
{
    private readonly IAvailabilityService _availability;
    private readonly ILogger<AvailabilityFunction> _logger;

    public AvailabilityFunction(IAvailabilityService availability, ILogger<AvailabilityFunction> logger)
    {
        _availability = availability;
        _logger = logger;
    }

    [Function("GetSpaceAvailability")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "spaces/{spaceId}/availability")] HttpRequest req,
        string spaceId,
        CancellationToken cancellationToken)
    {
        var caller = req.HttpContext.GetUserContext();
        if (caller is null) return ApiResults.Unauthorized();

        if (!DateOnly.TryParse(req.Query["startDate"], out var startDate))
            return ApiResults.BadRequest("startDate (yyyy-MM-dd) is required.");
        if (!DateOnly.TryParse(req.Query["endDate"], out var endDate))
            return ApiResults.BadRequest("endDate (yyyy-MM-dd) is required.");
        if (endDate < startDate)
            return ApiResults.BadRequest("endDate must be on or after startDate.");

        try
        {
            var availability = await _availability.GetAvailabilityAsync(spaceId, startDate, endDate, cancellationToken);
            return new OkObjectResult(availability);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogInformation(ex, "Availability lookup miss for space {SpaceId}", spaceId);
            return ApiResults.NotFound(ex.Message);
        }
    }
}
