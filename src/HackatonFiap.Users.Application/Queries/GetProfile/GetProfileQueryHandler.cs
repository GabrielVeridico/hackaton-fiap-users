using HackatonFiap.Users.Application.DTOs;
using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Abstractions;

namespace HackatonFiap.Users.Application.Queries.GetProfile;

public class GetProfileQueryHandler
{
    private readonly IUserRepository _userRepository;

    public GetProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserResponse>> HandleAsync(GetProfileQuery query)
    {
        var user = await _userRepository.FindByIdAsync(query.UserId);
        if (user is null)
            return Result<UserResponse>.Failure(UserErrors.NotFound);

        return Result<UserResponse>.Success(new UserResponse(
            user.Id, user.PersonType.ToString(), user.Document.Value, user.Name, user.Email,
            user.Role.ToString(), user.IsActive, user.IsOwner, user.CreatedAtUtc, user.UpdatedAtUtc));
    }
}
