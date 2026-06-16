using FluentValidation;

namespace FGC.Users.Application.Commands.UpdateProfile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(1).WithMessage("Name cannot be empty.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[a-zA-Z]").WithMessage("Password must contain at least one letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.")
            .When(x => x.Password is not null);
    }
}
