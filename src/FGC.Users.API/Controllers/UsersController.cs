using FGC.Users.API.Middlewares;
using FGC.Users.Application.Commands.CreateUser;
using FGC.Users.Application.Commands.UpdateProfile;
using FGC.Users.Application.Queries.GetProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FGC.Users.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly CreateUserCommandHandler _createUserHandler;
    private readonly GetProfileQueryHandler _getProfileHandler;
    private readonly UpdateProfileCommandHandler _updateProfileHandler;
    private readonly ICorrelationContext _correlation;

    public UsersController(
        CreateUserCommandHandler createUserHandler,
        GetProfileQueryHandler getProfileHandler,
        UpdateProfileCommandHandler updateProfileHandler,
        ICorrelationContext correlation)
    {
        _createUserHandler = createUserHandler;
        _getProfileHandler = getProfileHandler;
        _updateProfileHandler = updateProfileHandler;
        _correlation = correlation;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateUserCommand command)
    {
        var cmd = command with { CorrelationId = _correlation.CorrelationId };
        var result = await _createUserHandler.HandleAsync(cmd);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Title = result.Error.Description, Detail = result.Error.Code });

        return CreatedAtAction(nameof(GetMe), null, result.Value);
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

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var uid = GetUserId();
        if (uid is null) return Unauthorized();

        var command = new UpdateProfileCommand(uid.Value, request.Name, request.Password, _correlation.CorrelationId);
        var result = await _updateProfileHandler.HandleAsync(command);

        if (result.IsFailure)
            return result.Error.Code == "User.NotFound"
                ? NotFound(new ProblemDetails { Title = result.Error.Description })
                : BadRequest(new ProblemDetails { Title = result.Error.Description });

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

public record UpdateProfileRequest(string? Name, string? Password);
