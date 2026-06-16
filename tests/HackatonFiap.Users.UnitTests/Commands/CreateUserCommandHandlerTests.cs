using Xunit;
using HackatonFiap.Users.Application.Commands.CreateUser;
using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Entities;
using HackatonFiap.Users.Domain.Events;
using FluentAssertions;
using NSubstitute;

namespace HackatonFiap.Users.UnitTests.Commands;

public class CreateUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _handler = new CreateUserCommandHandler(
            _userRepository,
            _passwordHasher,
            _auditService,
            _eventPublisher);

        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed_password");
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldReturnSuccess()
    {
        var command = new CreateUserCommand("Test User", "test@example.com", "ValidPass123!", "corr-id");
        _userRepository.FindByEmailAsync(command.Email).Returns((User?)null);

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
        _userRepository.FindByEmailAsync(command.Email).Returns(existingUser);

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
        _userRepository.FindByEmailAsync(command.Email).Returns((User?)null);

        await _handler.HandleAsync(command);

        _passwordHasher.Received(1).Hash(command.Password);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallAuditService()
    {
        var command = new CreateUserCommand("Test User", "test@example.com", "ValidPass123!", "corr-id");
        _userRepository.FindByEmailAsync(command.Email).Returns((User?)null);

        await _handler.HandleAsync(command);

        await _auditService.Received(1).AuditAsync(
            Arg.Is("User"), Arg.Any<Guid>(), Arg.Is("UserRegistered"),
            Arg.Is<object?>(x => x == null), Arg.Any<object>(),
            Arg.Is(command.CorrelationId), Arg.Is<string?>(x => x == null));
    }

    [Fact]
    public async Task HandleAsync_ShouldCallEventPublisher()
    {
        var command = new CreateUserCommand("Test User", "test@example.com", "ValidPass123!", "corr-id");
        _userRepository.FindByEmailAsync(command.Email).Returns((User?)null);

        await _handler.HandleAsync(command);

        await _eventPublisher.Received(1).PublishAsync(
            "user-registered", Arg.Any<UserRegistered>(), command.CorrelationId);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallRepository_SaveNewAsync()
    {
        var command = new CreateUserCommand("Test User", "test@example.com", "ValidPass123!", "corr-id");
        _userRepository.FindByEmailAsync(command.Email).Returns((User?)null);

        await _handler.HandleAsync(command);

        await _userRepository.Received(1).SaveNewAsync(Arg.Any<User>());
    }
}
