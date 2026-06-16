using Xunit;
using HackatonFiap.Users.Domain.Entities;
using HackatonFiap.Users.Domain.Enums;
using HackatonFiap.Users.Domain.ValueObjects;
using FluentAssertions;

namespace HackatonFiap.Users.UnitTests.Domain;

public class UserEntityTests
{
    private static Document ValidCpf => Document.Create("52998224725", PersonType.Individual);

    [Fact]
    public void RegisterDonor_WithValidData_ShouldCreateUser()
    {
        var user = User.RegisterDonor(PersonType.Individual, ValidCpf, "Test User", "test@example.com", new Password("hashed"));

        user.Id.Should().NotBeEmpty();
        user.Name.Should().Be("Test User");
        user.Email.Should().Be("test@example.com");
        user.PersonType.Should().Be(PersonType.Individual);
        user.Role.Should().Be(UserRole.Doador);
        user.IsActive.Should().BeTrue();
        user.IsOwner.Should().BeFalse();
        user.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CreateOwner_ShouldBeGestorOngAndOwner()
    {
        var user = User.CreateOwner(ValidCpf, "Owner", "owner@example.com", new Password("hashed"));

        user.Role.Should().Be(UserRole.GestorONG);
        user.IsOwner.Should().BeTrue();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateProfile_ShouldChangeName()
    {
        var user = User.RegisterDonor(PersonType.Individual, ValidCpf, "Old Name", "test@example.com", new Password("hashed"));

        user.UpdateProfile("New Name");

        user.Name.Should().Be("New Name");
        user.UpdatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ChangeRole_ShouldChangeRole()
    {
        var user = User.RegisterDonor(PersonType.Individual, ValidCpf, "Test", "test@example.com", new Password("hashed"));

        user.ChangeRole(UserRole.GestorONG);

        user.Role.Should().Be(UserRole.GestorONG);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var user = User.RegisterDonor(PersonType.Individual, ValidCpf, "Test", "test@example.com", new Password("hashed"));

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Reactivate_ShouldSetIsActiveTrue()
    {
        var user = User.RegisterDonor(PersonType.Individual, ValidCpf, "Test", "test@example.com", new Password("hashed"));
        user.Deactivate();

        user.Reactivate();

        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RegisterDonor_DefaultRole_ShouldBeDoador()
    {
        var user = User.RegisterDonor(PersonType.Individual, ValidCpf, "Test", "test@example.com", new Password("hashed"));

        user.Role.Should().Be(UserRole.Doador);
    }
}
