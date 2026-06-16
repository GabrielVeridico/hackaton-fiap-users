using HackatonFiap.Users.Application.DTOs;
using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Abstractions;

namespace HackatonFiap.Users.Application.Queries.GetUserById;

public record GetUserByIdQuery(Guid UserId);

public class GetUserByIdQueryHandler
{
    private readonly IUserRepository _users;

    public GetUserByIdQueryHandler(IUserRepository users) => _users = users;

    public async Task<Result<UserResponse>> HandleAsync(GetUserByIdQuery q)
    {
        var user = await _users.FindByIdIncludingInactiveAsync(q.UserId);
        return user is null
            ? Result<UserResponse>.Failure(UserErrors.NotFound)
            : Result<UserResponse>.Success(UserResponse.From(user));
    }
}
