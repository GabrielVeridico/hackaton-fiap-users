using HackatonFiap.Users.API.Middlewares;
using HackatonFiap.Users.Application.Commands.ChangeUserRole;
using HackatonFiap.Users.Application.Commands.CreateUser;
using HackatonFiap.Users.Application.Commands.DeactivateUser;
using HackatonFiap.Users.Application.Commands.ReactivateUser;
using HackatonFiap.Users.Application.Commands.ResetMyPassword;
using HackatonFiap.Users.Application.Commands.UpdateMyProfile;
using HackatonFiap.Users.Application.Commands.UpdateUser;
using HackatonFiap.Users.Application.Queries.GetProfile;
using HackatonFiap.Users.Application.Queries.GetUserById;
using HackatonFiap.Users.Application.Queries.ListUsers;
using HackatonFiap.Users.Domain.Abstractions;
using HackatonFiap.Users.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HackatonFiap.Users.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly GetProfileQueryHandler _getProfile;
    private readonly GetUserByIdQueryHandler _getUserById;
    private readonly ListUsersQueryHandler _listUsers;
    private readonly CreateUserCommandHandler _createUser;
    private readonly UpdateUserCommandHandler _updateUser;
    private readonly ChangeUserRoleCommandHandler _changeRole;
    private readonly DeactivateUserCommandHandler _deactivate;
    private readonly ReactivateUserCommandHandler _reactivate;
    private readonly UpdateMyProfileCommandHandler _updateMyProfile;
    private readonly ResetMyPasswordCommandHandler _resetMyPassword;
    private readonly ICorrelationContext _correlation;

    public UsersController(
        GetProfileQueryHandler getProfile,
        GetUserByIdQueryHandler getUserById,
        ListUsersQueryHandler listUsers,
        CreateUserCommandHandler createUser,
        UpdateUserCommandHandler updateUser,
        ChangeUserRoleCommandHandler changeRole,
        DeactivateUserCommandHandler deactivate,
        ReactivateUserCommandHandler reactivate,
        UpdateMyProfileCommandHandler updateMyProfile,
        ResetMyPasswordCommandHandler resetMyPassword,
        ICorrelationContext correlation)
    {
        _getProfile = getProfile;
        _getUserById = getUserById;
        _listUsers = listUsers;
        _createUser = createUser;
        _updateUser = updateUser;
        _changeRole = changeRole;
        _deactivate = deactivate;
        _reactivate = reactivate;
        _updateMyProfile = updateMyProfile;
        _resetMyPassword = resetMyPassword;
        _correlation = correlation;
    }

    // ---------- Self-service (authenticated) ----------

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var uid = CallerId();
        if (uid is null) return Unauthorized();
        var result = await _getProfile.HandleAsync(new GetProfileQuery(uid.Value));
        return result.IsFailure ? MapError(result.Error) : Ok(result.Value);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest request)
    {
        var uid = CallerId();
        if (uid is null) return Unauthorized();
        var result = await _updateMyProfile.HandleAsync(
            new UpdateMyProfileCommand(uid.Value, request.Name, _correlation.CorrelationId));
        return result.IsFailure ? MapError(result.Error) : Ok(result.Value);
    }

    [Authorize]
    [HttpPost("me/reset-password")]
    public async Task<IActionResult> ResetMyPasswordEndpoint([FromBody] ResetPasswordRequest request)
    {
        var uid = CallerId();
        if (uid is null) return Unauthorized();
        var result = await _resetMyPassword.HandleAsync(
            new ResetMyPasswordCommand(uid.Value, request.CurrentPassword, request.NewPassword, _correlation.CorrelationId));
        return result.IsFailure ? MapError(result.Error) : NoContent();
    }

    // ---------- Management (GestorONG) ----------

    [Authorize(Roles = "GestorONG")]
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var result = await _listUsers.HandleAsync(new ListUsersQuery());
        return Ok(result.Value);
    }

    [Authorize(Roles = "GestorONG")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _getUserById.HandleAsync(new GetUserByIdQuery(id));
        return result.IsFailure ? MapError(result.Error) : Ok(result.Value);
    }

    [Authorize(Roles = "GestorONG")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var result = await _createUser.HandleAsync(new CreateUserCommand(
            request.PersonType, request.Document, request.Name, request.Email, request.Password,
            request.Role, CallerIsOwner(), _correlation.CorrelationId));
        if (result.IsFailure) return MapError(result.Error);
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [Authorize(Roles = "GestorONG")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var result = await _updateUser.HandleAsync(
            new UpdateUserCommand(id, request.Name, CallerIsOwner(), _correlation.CorrelationId));
        return result.IsFailure ? MapError(result.Error) : Ok(result.Value);
    }

    [Authorize(Roles = "GestorONG")]
    [HttpPatch("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest request)
    {
        var result = await _changeRole.HandleAsync(
            new ChangeUserRoleCommand(id, request.Role, CallerIsOwner(), _correlation.CorrelationId));
        return result.IsFailure ? MapError(result.Error) : NoContent();
    }

    [Authorize(Roles = "GestorONG")]
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _deactivate.HandleAsync(
            new DeactivateUserCommand(id, CallerIsOwner(), _correlation.CorrelationId));
        return result.IsFailure ? MapError(result.Error) : NoContent();
    }

    [Authorize(Roles = "GestorONG")]
    [HttpPatch("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        var result = await _reactivate.HandleAsync(
            new ReactivateUserCommand(id, CallerIsOwner(), _correlation.CorrelationId));
        return result.IsFailure ? MapError(result.Error) : NoContent();
    }

    // ---------- Helpers ----------

    private Guid? CallerId()
    {
        var sub = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private bool CallerIsOwner() => User.FindFirst("isOwner")?.Value == "true";

    private IActionResult MapError(Error error) => error.Code switch
    {
        "User.Forbidden" or "User.OwnerImmutable" or "User.CannotManageGestor"
            => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Title = error.Description, Detail = error.Code }),
        "User.NotFound"
            => NotFound(new ProblemDetails { Title = error.Description, Detail = error.Code }),
        "User.AlreadyRegistered" or "User.InactiveAccount"
            => Conflict(new ProblemDetails { Title = error.Description, Detail = error.Code }),
        _ => BadRequest(new ProblemDetails { Title = error.Description, Detail = error.Code })
    };
}

public record UpdateMeRequest(string Name);
public record ResetPasswordRequest(string CurrentPassword, string NewPassword);
public record CreateUserRequest(PersonType PersonType, string Document, string Name, string Email, string Password, UserRole Role);
public record UpdateUserRequest(string Name);
public record ChangeRoleRequest(UserRole Role);
