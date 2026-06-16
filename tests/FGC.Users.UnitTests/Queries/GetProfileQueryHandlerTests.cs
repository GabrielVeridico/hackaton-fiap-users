using Xunit;
using FGC.Users.Application.Errors;
using FGC.Users.Application.Interfaces;
using FGC.Users.Application.Queries.GetProfile;
using FGC.Users.Domain.Entities;
using FGC.Users.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace FGC.Users.UnitTests.Queries;

public class GetProfileQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly GetProfileQueryHandler _handler;

    public GetProfileQueryHandlerTests()
    {
        _handler = new GetProfileQueryHandler(_userRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidId_ShouldReturnProfile()
    {
        var user = User.Create("Test User", "test@example.com", new Password("hashed"));
        _userRepository.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _handler.HandleAsync(new GetProfileQuery(user.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("test@example.com");
        result.Value.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task HandleAsync_WithInvalidId_ShouldReturnNotFound()
    {
        var id = Guid.NewGuid();
        _userRepository.Setup(x => x.FindByIdAsync(id)).ReturnsAsync((User?)null);

        var result = await _handler.HandleAsync(new GetProfileQuery(id));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.NotFound.Code);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCorrectFields()
    {
        var user = User.Create("Full Name", "full@example.com", new Password("hashed"));
        _userRepository.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _handler.HandleAsync(new GetProfileQuery(user.Id));

        result.Value.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be("full@example.com");
        result.Value.Name.Should().Be("Full Name");
        result.Value.Role.Should().Be("User");
        result.Value.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Value.UpdatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
