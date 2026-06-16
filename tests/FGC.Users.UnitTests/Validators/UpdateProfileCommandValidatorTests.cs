using Xunit;
using FGC.Users.Application.Commands.UpdateProfile;
using FluentValidation.TestHelper;

namespace FGC.Users.UnitTests.Validators;

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var command = new UpdateProfileCommand(Guid.NewGuid(), "New Name", "NewPass123!", "corr-id");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNullOptionalFields_ShouldPass()
    {
        var command = new UpdateProfileCommand(Guid.NewGuid(), null, null, "corr-id");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithWeakPassword_ShouldFail()
    {
        var command = new UpdateProfileCommand(Guid.NewGuid(), "Name", "weak", "corr-id");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldFail()
    {
        var command = new UpdateProfileCommand(Guid.NewGuid(), "", null, "corr-id");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
