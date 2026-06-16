using FGC.Users.Application.DTOs;
using FGC.Users.Application.Errors;
using FGC.Users.Application.Interfaces;
using FGC.Users.Domain.Abstractions;

namespace FGC.Users.Application.Queries.GetProfile;

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
            user.Id, user.Email, user.Name, user.Role.ToString(),
            user.CreatedAtUtc, user.UpdatedAtUtc));
    }
}
