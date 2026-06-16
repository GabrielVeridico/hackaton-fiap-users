using Xunit;
using HackatonFiap.Users.Domain.Entities;
using HackatonFiap.Users.Domain.Enums;
using HackatonFiap.Users.Domain.ValueObjects;
using FluentAssertions;

namespace HackatonFiap.Users.UnitTests.Domain;

public class UserEntityTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateUser()
    {
        var user = User.Create("Test User", "test@example.com", new Password("hashed"));

        user.Id.Should().NotBeEmpty();
        user.Name.Should().Be("Test User");
        user.Email.Should().Be("test@example.com");
        user.IsActive.Should().BeTrue();
        user.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdateProfile_ShouldChangeName()
    {
        var user = User.Create("Old Name", "test@example.com", new Password("hashed"));

        user.UpdateProfile("New Name");

        user.Name.Should().Be("New Name");
        user.UpdatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var user = User.Create("Test", "test@example.com", new Password("hashed"));

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Create_DefaultRole_ShouldBeDoador()
    {
        var user = User.Create("Test", "test@example.com", new Password("hashed"));

        user.Role.Should().Be(UserRole.Doador);
    }
}
