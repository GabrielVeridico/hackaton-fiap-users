using FluentValidation;

namespace HackatonFiap.Users.Application.Commands.RegisterDonor;

public class RegisterDonorCommandValidator : AbstractValidator<RegisterDonorCommand>
{
    public RegisterDonorCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Document).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
