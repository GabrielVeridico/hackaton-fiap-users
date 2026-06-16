using Xunit;
using FGC.Users.Application.Commands.UpdateProfile;
using FGC.Users.Application.Errors;
using FGC.Users.Application.Interfaces;
using FGC.Users.Domain.Entities;
using FGC.Users.Domain.Events;
using FGC.Users.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace FGC.Users.UnitTests.Commands;

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileCommandHandlerTests()
    {
        _handler = new UpdateProfileCommandHandler(
            _userRepository.Object,
            _passwordHasher.Object,
            _auditService.Object,
            _eventPublisher.Object);

        _passwordHasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("new_hashed_password");
    }

    [Fact]
    public async Task HandleAsync_UpdateName_ShouldReturnSuccess()
    {
        var user = User.Create("Old Name", "test@example.com", new Password("hashed"));
        var command = new UpdateProfileCommand(user.Id, "New Name", null, "corr-id");
        _userRepository.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task HandleAsync_UpdatePassword_ShouldReturnSuccess()
    {
        var user = User.Create("Test User", "test@example.com", new Password("old_hashed"));
        var command = new UpdateProfileCommand(user.Id, null, "NewValidPass123!", "corr-id");
        _userRepository.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        _passwordHasher.Verify(x => x.Hash("NewValidPass123!"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NonExistentUser_ShouldReturnNotFound()
    {
        var command = new UpdateProfileCommand(Guid.NewGuid(), "Name", null, "corr-id");
        _userRepository.Setup(x => x.FindByIdAsync(command.UserId)).ReturnsAsync((User?)null);

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.NotFound.Code);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallAuditService()
    {
        var user = User.Create("Old Name", "test@example.com", new Password("hashed"));
        var command = new UpdateProfileCommand(user.Id, "New Name", null, "corr-id");
        _userRepository.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);

        await _handler.HandleAsync(command);

        _auditService.Verify(x => x.AuditAsync(
            "User", user.Id, "UserProfileUpdated",
            It.IsAny<object>(), It.IsAny<object>(),
            command.CorrelationId, user.Id.ToString()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallEventPublisher()
    {
        var user = User.Create("Old Name", "test@example.com", new Password("hashed"));
        var command = new UpdateProfileCommand(user.Id, "New Name", null, "corr-id");
        _userRepository.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);

        await _handler.HandleAsync(command);

        _eventPublisher.Verify(x => x.PublishAsync(
            "user-profile-updated", It.IsAny<UserProfileUpdated>(), command.CorrelationId), Times.Once);
    }
}
