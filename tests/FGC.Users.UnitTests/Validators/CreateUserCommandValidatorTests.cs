using Xunit;
using FGC.Users.Application.Commands.CreateUser;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace FGC.Users.UnitTests.Validators;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var command = new CreateUserCommand("Test User", "test@example.com", "ValidPass123!", "corr-id");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldFail()
    {
        var command = new CreateUserCommand("", "test@example.com", "ValidPass123!", "corr-id");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithEmptyEmail_ShouldFail()
    {
        var command = new CreateUserCommand("Test", "", "ValidPass123!", "corr-id");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithInvalidEmail_ShouldFail()
    {
        var command = new CreateUserCommand("Test", "invalid-email", "ValidPass123!", "corr-id");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithEmptyPassword_ShouldFail()
    {
        var command = new CreateUserCommand("Test", "test@example.com", "", "corr-id");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WithShortPassword_ShouldFail()
    {
        var command = new CreateUserCommand("Test", "test@example.com", "Ab1!", "corr-id");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WithPasswordWithoutLetter_ShouldFail()
    {
        var command = new CreateUserCommand("Test", "test@example.com", "12345678!", "corr-id");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WithPasswordWithoutDigit_ShouldFail()
    {
        var command = new CreateUserCommand("Test", "test@example.com", "Password!!", "corr-id");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WithPasswordWithoutSpecialChar_ShouldFail()
    {
        var command = new CreateUserCommand("Test", "test@example.com", "Password12", "corr-id");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WithMultipleInvalidFields_ShouldFailForAll()
    {
        var command = new CreateUserCommand("", "invalid", "short", "corr-id");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
