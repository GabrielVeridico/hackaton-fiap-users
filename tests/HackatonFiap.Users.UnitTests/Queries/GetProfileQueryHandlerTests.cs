using Xunit;
using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Application.Queries.GetProfile;
using HackatonFiap.Users.Domain.Entities;
using HackatonFiap.Users.Domain.Enums;
using HackatonFiap.Users.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace HackatonFiap.Users.UnitTests.Queries;

public class GetProfileQueryHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly GetProfileQueryHandler _handler;

    public GetProfileQueryHandlerTests()
    {
        _handler = new GetProfileQueryHandler(_userRepository);
    }

    [Fact]
    public async Task HandleAsync_WithValidId_ShouldReturnProfile()
    {
        var user = User.RegisterDonor(PersonType.Individual, Document.Create("52998224725", PersonType.Individual), "Test User", "test@example.com", new Password("hashed"));
        _userRepository.FindByIdAsync(user.Id).Returns(user);

        var result = await _handler.HandleAsync(new GetProfileQuery(user.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("test@example.com");
        result.Value.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task HandleAsync_WithInvalidId_ShouldReturnNotFound()
    {
        var id = Guid.NewGuid();
        _userRepository.FindByIdAsync(id).Returns((User?)null);

        var result = await _handler.HandleAsync(new GetProfileQuery(id));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.NotFound.Code);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCorrectFields()
    {
        var user = User.RegisterDonor(PersonType.Individual, Document.Create("52998224725", PersonType.Individual), "Full Name", "full@example.com", new Password("hashed"));
        _userRepository.FindByIdAsync(user.Id).Returns(user);

        var result = await _handler.HandleAsync(new GetProfileQuery(user.Id));

        result.Value.Id.Should().Be(user.Id);
        result.Value.PersonType.Should().Be("Individual");
        result.Value.Document.Should().Be("52998224725");
        result.Value.Email.Should().Be("full@example.com");
        result.Value.Name.Should().Be("Full Name");
        result.Value.Role.Should().Be("Doador");
        result.Value.IsActive.Should().BeTrue();
        result.Value.IsOwner.Should().BeFalse();
        result.Value.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Value.UpdatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
