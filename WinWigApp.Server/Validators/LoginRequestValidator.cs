using FluentValidation;
using WinWigApp.Server.DTOs;

namespace WinWigApp.Server.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email jest wymagany")
            .EmailAddress()
            .WithMessage("Email musi być prawidłowy");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Hasło jest wymagane");
    }
}
