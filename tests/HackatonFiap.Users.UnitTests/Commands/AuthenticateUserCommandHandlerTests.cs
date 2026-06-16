using Xunit;
using HackatonFiap.Users.Application.Commands.AuthenticateUser;
using HackatonFiap.Users.Application.DTOs;
using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Entities;
using HackatonFiap.Users.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace HackatonFiap.Users.UnitTests.Commands;

public class AuthenticateUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly AuthenticateUserCommandHandler _handler;

    public AuthenticateUserCommandHandlerTests()
    {
        _handler = new AuthenticateUserCommandHandler(
            _userRepository,
            _passwordHasher,
            _jwtTokenGenerator);
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ShouldReturnLoginResponse()
    {
        var user = User.Create("Test", "test@example.com", new Password("hashed"));
        var command = new AuthenticateUserCommand("test@example.com", "password123", "corr-id");
        var expectedResponse = new LoginResponse("jwt-token", DateTime.UtcNow.AddHours(1));

        _userRepository.FindByEmailAsync(command.Email).Returns(user);
        _passwordHasher.Verify(command.Password, "hashed").Returns(true);
        _jwtTokenGenerator.GenerateToken(user).Returns(expectedResponse);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("jwt-token");
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentEmail_ShouldReturnInvalidCredentials()
    {
        var command = new AuthenticateUserCommand("notfound@example.com", "password123", "corr-id");
        _userRepository.FindByEmailAsync(command.Email).Returns((User?)null);

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.InvalidCredentials.Code);
    }

    [Fact]
    public async Task HandleAsync_WithWrongPassword_ShouldReturnInvalidCredentials()
    {
        var user = User.Create("Test", "test@example.com", new Password("hashed"));
        var command = new AuthenticateUserCommand("test@example.com", "wrongpassword", "corr-id");

        _userRepository.FindByEmailAsync(command.Email).Returns(user);
        _passwordHasher.Verify(command.Password, "hashed").Returns(false);

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.InvalidCredentials.Code);
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ShouldCallJwtTokenGenerator()
    {
        var user = User.Create("Test", "test@example.com", new Password("hashed"));
        var command = new AuthenticateUserCommand("test@example.com", "password123", "corr-id");
        var expectedResponse = new LoginResponse("jwt-token", DateTime.UtcNow.AddHours(1));

        _userRepository.FindByEmailAsync(command.Email).Returns(user);
        _passwordHasher.Verify(command.Password, "hashed").Returns(true);
        _jwtTokenGenerator.GenerateToken(user).Returns(expectedResponse);

        await _handler.HandleAsync(command);

        _jwtTokenGenerator.Received(1).GenerateToken(user);
    }
}
