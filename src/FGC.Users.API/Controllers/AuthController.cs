using FGC.Users.API.Middlewares;
using FGC.Users.Application.Commands.AuthenticateUser;
using Microsoft.AspNetCore.Mvc;

namespace FGC.Users.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthenticateUserCommandHandler _authenticateHandler;
    private readonly ICorrelationContext _correlation;

    public AuthController(
        AuthenticateUserCommandHandler authenticateHandler,
        ICorrelationContext correlation)
    {
        _authenticateHandler = authenticateHandler;
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
}

public record LoginRequest(string Email, string Password);
