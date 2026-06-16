using HackatonFiap.Users.Application.DTOs;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Abstractions;

namespace HackatonFiap.Users.Application.Queries.ListUsers;

public record ListUsersQuery();

public class ListUsersQueryHandler
{
    private readonly IUserRepository _users;

    public ListUsersQueryHandler(IUserRepository users) => _users = users;

    public async Task<Result<IReadOnlyList<UserResponse>>> HandleAsync(ListUsersQuery q)
    {
        var users = await _users.ListAsync();
        return Result<IReadOnlyList<UserResponse>>.Success(users.Select(UserResponse.From).ToList());
    }
}
