using HackatonFiap.Users.API.Middlewares;
using HackatonFiap.Users.Application.Commands.AuthenticateUser;
using HackatonFiap.Users.Application.Commands.RefreshTokenFlow;
using HackatonFiap.Users.Application.Commands.RegisterDonor;
using Microsoft.AspNetCore.Mvc;

namespace HackatonFiap.Users.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthenticateUserCommandHandler _authenticateHandler;
    private readonly RegisterDonorCommandHandler _registerDonorHandler;
    private readonly RefreshTokenCommandHandler _refreshHandler;
    private readonly ICorrelationContext _correlation;

    public AuthController(
        AuthenticateUserCommandHandler authenticateHandler,
        RegisterDonorCommandHandler registerDonorHandler,
        RefreshTokenCommandHandler refreshHandler,
        ICorrelationContext correlation)
    {
        _authenticateHandler = authenticateHandler;
        _registerDonorHandler = registerDonorHandler;
        _refreshHandler = refreshHandler;
        _correlation = correlation;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new AuthenticateUserCommand(request.Email, request.Password, _correlation.CorrelationId);
        var result = await _authenticateHandler.HandleAsync(command);

        if (result.IsFailure)
            return Unauthorized(new ProblemDetails { Title = result.Error.Description });

        return Ok(result.Value);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDonorRequest request)
    {
        var command = new RegisterDonorCommand(
            request.PersonType, request.Document, request.Name, request.Email, request.Password, _correlation.CorrelationId);
        var result = await _registerDonorHandler.HandleAsync(command);

        if (result.IsFailure)
        {
            if (result.Error.Code is "User.AlreadyRegistered" or "User.InactiveAccount")
                return Conflict(new ProblemDetails { Title = result.Error.Description, Detail = result.Error.Code });
            return BadRequest(new ProblemDetails { Title = result.Error.Description, Detail = result.Error.Code });
        }
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _refreshHandler.HandleAsync(new RefreshTokenCommand(request.RefreshToken, _correlation.CorrelationId));
        if (result.IsFailure)
            return Unauthorized(new ProblemDetails { Title = result.Error.Description, Detail = result.Error.Code });
        return Ok(result.Value);
    }
}

public record LoginRequest(string Email, string Password);
public record RegisterDonorRequest(HackatonFiap.Users.Domain.Enums.PersonType PersonType, string Document, string Name, string Email, string Password);
public record RefreshRequest(string RefreshToken);
