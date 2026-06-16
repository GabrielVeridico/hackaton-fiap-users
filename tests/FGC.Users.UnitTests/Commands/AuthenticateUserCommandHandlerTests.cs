using Xunit;
using FGC.Users.Application.Commands.AuthenticateUser;
using FGC.Users.Application.DTOs;
using FGC.Users.Application.Errors;
using FGC.Users.Application.Interfaces;
using FGC.Users.Domain.Entities;
using FGC.Users.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace FGC.Users.UnitTests.Commands;

public class AuthenticateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly AuthenticateUserCommandHandler _handler;

    public AuthenticateUserCommandHandlerTests()
    {
        _handler = new AuthenticateUserCommandHandler(
            _userRepository.Object,
            _passwordHasher.Object,
            _jwtTokenGenerator.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ShouldReturnLoginResponse()
    {
        var user = User.Create("Test", "test@example.com", new Password("hashed"));
        var command = new AuthenticateUserCommand("test@example.com", "password123", "corr-id");
        var expectedResponse = new LoginResponse("jwt-token", DateTime.UtcNow.AddHours(1));

        _userRepository.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify(command.Password, "hashed")).Returns(true);
        _jwtTokenGenerator.Setup(x => x.GenerateToken(user)).Returns(expectedResponse);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("jwt-token");
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentEmail_ShouldReturnInvalidCredentials()
    {
        var command = new AuthenticateUserCommand("notfound@example.com", "password123", "corr-id");
        _userRepository.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User?)null);

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.InvalidCredentials.Code);
    }

    [Fact]
    public async Task HandleAsync_WithWrongPassword_ShouldReturnInvalidCredentials()
    {
        var user = User.Create("Test", "test@example.com", new Password("hashed"));
        var command = new AuthenticateUserCommand("test@example.com", "wrongpassword", "corr-id");

        _userRepository.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify(command.Password, "hashed")).Returns(false);

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

        _userRepository.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify(command.Password, "hashed")).Returns(true);
        _jwtTokenGenerator.Setup(x => x.GenerateToken(user)).Returns(expectedResponse);

        await _handler.HandleAsync(command);

        _jwtTokenGenerator.Verify(x => x.GenerateToken(user), Times.Once);
    }
}
