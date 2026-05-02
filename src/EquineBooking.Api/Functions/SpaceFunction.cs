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

/// <summary>CRUD endpoints over <see cref="Space"/>. Mutations are admin-only.</summary>
public sealed class SpaceFunction
{
    private readonly ISpaceRepository _spaces;
    private readonly AuthorizationHelper _auth;
    private readonly ILogger<SpaceFunction> _logger;

    public SpaceFunction(ISpaceRepository spaces, AuthorizationHelper auth, ILogger<SpaceFunction> logger)
    {
        _spaces = spaces;
        _auth = auth;
        _logger = logger;
    }

    [Function("ListSpaces")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "spaces")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var caller = req.HttpContext.GetUserContext();
        if (caller is null) return ApiResults.Unauthorized();

        var spaces = await _spaces.GetActiveAsync(cancellationToken);
        return new OkObjectResult(spaces);
    }

    [Function("GetSpace")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "spaces/{id}")] HttpRequest req,
        string id,
        CancellationToken cancellationToken)
    {
        var caller = req.HttpContext.GetUserContext();
        if (caller is null) return ApiResults.Unauthorized();

        var space = await _spaces.GetByIdAsync(id, cancellationToken);
        return space is null
            ? ApiResults.NotFound($"Space '{id}' not found.")
            : new OkObjectResult(space);
    }

    [Function("CreateSpace")]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "spaces")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var caller = req.HttpContext.GetUserContext();
        if (caller is null) return ApiResults.Unauthorized();
        if (!await _auth.IsAdminAsync(caller, cancellationToken)) return ApiResults.Forbidden();

        Space? body;
        try
        {
            body = await req.ReadFromJsonAsync<Space>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Malformed Space body");
            return ApiResults.BadRequest("Request body could not be parsed.");
        }
        if (body is null) return ApiResults.BadRequest("Request body is required.");
        if (string.IsNullOrWhiteSpace(body.Id)) body.Id = Guid.NewGuid().ToString();
        if (string.IsNullOrWhiteSpace(body.Name)) return ApiResults.BadRequest("name is required.");

        try
        {
            var created = await _spaces.CreateAsync(body, cancellationToken);
            return new ObjectResult(created) { StatusCode = StatusCodes.Status201Created };
        }
        catch (ConflictException ex)
        {
            return ApiResults.Conflict(ex.Message);
        }
    }

    [Function("UpdateSpace")]
    public async Task<IActionResult> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "spaces/{id}")] HttpRequest req,
        string id,
        CancellationToken cancellationToken)
    {
        var caller = req.HttpContext.GetUserContext();
        if (caller is null) return ApiResults.Unauthorized();
        if (!await _auth.IsAdminAsync(caller, cancellationToken)) return ApiResults.Forbidden();

        var existing = await _spaces.GetByIdAsync(id, cancellationToken);
        if (existing is null) return ApiResults.NotFound($"Space '{id}' not found.");

        Space? body;
        try
        {
            body = await req.ReadFromJsonAsync<Space>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Malformed Space body");
            return ApiResults.BadRequest("Request body could not be parsed.");
        }
        if (body is null) return ApiResults.BadRequest("Request body is required.");

        body.Id = id;
        try
        {
            var updated = await _spaces.UpdateAsync(body, cancellationToken);
            return new OkObjectResult(updated);
        }
        catch (ConflictException ex)
        {
            return ApiResults.Conflict(ex.Message);
        }
    }

    [Function("DeleteSpace")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "spaces/{id}")] HttpRequest req,
        string id,
        CancellationToken cancellationToken)
    {
        var caller = req.HttpContext.GetUserContext();
        if (caller is null) return ApiResults.Unauthorized();
        if (!await _auth.IsAdminAsync(caller, cancellationToken)) return ApiResults.Forbidden();

        await _spaces.DeleteAsync(id, cancellationToken);
        return new NoContentResult();
    }
}
