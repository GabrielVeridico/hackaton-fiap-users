using Xunit;
using FGC.Users.Application.Commands.CreateUser;
using FGC.Users.Application.Errors;
using FGC.Users.Application.Interfaces;
using FGC.Users.Domain.Entities;
using FGC.Users.Domain.Events;
using FluentAssertions;
using Moq;

namespace FGC.Users.UnitTests.Commands;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _handler = new CreateUserCommandHandler(
            _userRepository.Object,
            _passwordHasher.Object,
            _auditService.Object,
            _eventPublisher.Object);

        _passwordHasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed_password");
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldReturnSuccess()
    {
        var command = new CreateUserCommand("Test User", "test@example.com", "ValidPass123!", "corr-id");
        _userRepository.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User?)null);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(command.Email);
        result.Value.Name.Should().Be(command.Name);
        result.Value.Role.Should().Be("User");
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateEmail_ShouldReturnEmailAlreadyRegistered()
    {
        var command = new CreateUserCommand("Test User", "existing@example.com", "ValidPass123!", "corr-id");
        var existingUser = Fixtures.TestData.Users.ValidUser;
        _userRepository.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(existingUser);

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.EmailAlreadyRegistered.Code);
    }

    [Fact]
    public async Task HandleAsync_WithWeakPassword_ShouldReturnInvalidPassword()
    {
        var command = new CreateUserCommand("Test User", "test@example.com", "weak", "corr-id");

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.InvalidPassword.Code);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallPasswordHasher()
    {
        var command = new CreateUserCommand("Test User", "test@example.com", "ValidPass123!", "corr-id");
        _userRepository.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User?)null);

        await _handler.HandleAsync(command);

        _passwordHasher.Verify(x => x.Hash(command.Password), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallAuditService()
    {
        var command = new CreateUserCommand("Test User", "test@example.com", "ValidPass123!", "corr-id");
        _userRepository.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User?)null);

        await _handler.HandleAsync(command);

        _auditService.Verify(x => x.AuditAsync(
            "User", It.IsAny<Guid>(), "UserRegistered",
            null, It.IsAny<object>(),
            command.CorrelationId, null), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallEventPublisher()
    {
        var command = new CreateUserCommand("Test User", "test@example.com", "ValidPass123!", "corr-id");
        _userRepository.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User?)null);

        await _handler.HandleAsync(command);

        _eventPublisher.Verify(x => x.PublishAsync(
            "user-registered", It.IsAny<UserRegistered>(), command.CorrelationId), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallRepository_SaveNewAsync()
    {
        var command = new CreateUserCommand("Test User", "test@example.com", "ValidPass123!", "corr-id");
        _userRepository.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((User?)null);

        await _handler.HandleAsync(command);

        _userRepository.Verify(x => x.SaveNewAsync(It.IsAny<User>()), Times.Once);
    }
}
