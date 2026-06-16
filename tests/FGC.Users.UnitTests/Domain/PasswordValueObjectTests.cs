using Xunit;
using FGC.Users.Domain.ValueObjects;
using FluentAssertions;

namespace FGC.Users.UnitTests.Domain;

public class PasswordValueObjectTests
{
    [Fact]
    public void IsValid_WithStrongPassword_ShouldReturnTrue()
    {
        Password.IsValid("ValidPass123!").Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithShortPassword_ShouldReturnFalse()
    {
        Password.IsValid("Ab1!").Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithoutLetter_ShouldReturnFalse()
    {
        Password.IsValid("12345678!").Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithoutDigit_ShouldReturnFalse()
    {
        Password.IsValid("Password!!").Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithoutSpecialChar_ShouldReturnFalse()
    {
        Password.IsValid("Password12").Should().BeFalse();
    }
}
