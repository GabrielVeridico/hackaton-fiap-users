using Xunit;
using HackatonFiap.Users.Application.Commands.UpdateProfile;
using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Entities;
using HackatonFiap.Users.Domain.Events;
using HackatonFiap.Users.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace HackatonFiap.Users.UnitTests.Commands;

public class UpdateProfileCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly IEventPublisher _eventPublisher = Substitute.For<IEventPublisher>();
    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileCommandHandlerTests()
    {
        _handler = new UpdateProfileCommandHandler(
            _userRepository,
            _passwordHasher,
            _auditService,
            _eventPublisher);

        _passwordHasher.Hash(Arg.Any<string>()).Returns("new_hashed_password");
    }

    [Fact]
    public async Task HandleAsync_UpdateName_ShouldReturnSuccess()
    {
        var user = User.Create("Old Name", "test@example.com", new Password("hashed"));
        var command = new UpdateProfileCommand(user.Id, "New Name", null, "corr-id");
        _userRepository.FindByIdAsync(user.Id).Returns(user);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task HandleAsync_UpdatePassword_ShouldReturnSuccess()
    {
        var user = User.Create("Test User", "test@example.com", new Password("old_hashed"));
        var command = new UpdateProfileCommand(user.Id, null, "NewValidPass123!", "corr-id");
        _userRepository.FindByIdAsync(user.Id).Returns(user);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        _passwordHasher.Received(1).Hash("NewValidPass123!");
    }

    [Fact]
    public async Task HandleAsync_NonExistentUser_ShouldReturnNotFound()
    {
        var command = new UpdateProfileCommand(Guid.NewGuid(), "Name", null, "corr-id");
        _userRepository.FindByIdAsync(command.UserId).Returns((User?)null);

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.NotFound.Code);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallAuditService()
    {
        var user = User.Create("Old Name", "test@example.com", new Password("hashed"));
        var command = new UpdateProfileCommand(user.Id, "New Name", null, "corr-id");
        _userRepository.FindByIdAsync(user.Id).Returns(user);

        await _handler.HandleAsync(command);

        await _auditService.Received(1).AuditAsync(
            "User", user.Id, "UserProfileUpdated",
            Arg.Any<object>(), Arg.Any<object>(),
            command.CorrelationId, user.Id.ToString());
    }

    [Fact]
    public async Task HandleAsync_ShouldCallEventPublisher()
    {
        var user = User.Create("Old Name", "test@example.com", new Password("hashed"));
        var command = new UpdateProfileCommand(user.Id, "New Name", null, "corr-id");
        _userRepository.FindByIdAsync(user.Id).Returns(user);

        await _handler.HandleAsync(command);

        await _eventPublisher.Received(1).PublishAsync(
            "user-profile-updated", Arg.Any<UserProfileUpdated>(), command.CorrelationId);
    }
}
