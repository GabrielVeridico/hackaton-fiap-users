using HackatonFiap.Users.API.Middlewares;
using HackatonFiap.Users.Application.Queries.GetProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HackatonFiap.Users.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly GetProfileQueryHandler _getProfileHandler;
    private readonly ICorrelationContext _correlation;

    public UsersController(
        GetProfileQueryHandler getProfileHandler,
        ICorrelationContext correlation)
    {
        _getProfileHandler = getProfileHandler;
        _correlation = correlation;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var uid = GetUserId();
        if (uid is null) return Unauthorized();

        var result = await _getProfileHandler.HandleAsync(new GetProfileQuery(uid.Value));

        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = result.Error.Description });

        return Ok(result.Value);
    }

    private Guid? GetUserId()
    {
        var uid = User.FindFirst("sub")?.Value
            ?? User.Identity?.Name
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(uid, out var id) ? id : null;
    }
}
