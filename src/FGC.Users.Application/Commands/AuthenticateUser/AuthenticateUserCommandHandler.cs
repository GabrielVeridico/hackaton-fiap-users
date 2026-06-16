using FGC.Users.Application.DTOs;
using FGC.Users.Application.Errors;
using FGC.Users.Application.Interfaces;
using FGC.Users.Domain.Abstractions;

namespace FGC.Users.Application.Commands.AuthenticateUser;

public class AuthenticateUserCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthenticateUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<LoginResponse>> HandleAsync(AuthenticateUserCommand command)
    {
        var user = await _userRepository.FindByEmailAsync(command.Email);
        if (user is null)
            return Result<LoginResponse>.Failure(UserErrors.InvalidCredentials);

        if (!_passwordHasher.Verify(command.Password, user.Password.HashValue))
            return Result<LoginResponse>.Failure(UserErrors.InvalidCredentials);

        var response = _jwtTokenGenerator.GenerateToken(user);
        return Result<LoginResponse>.Success(response);
    }
}
