using FluentValidation;
using WinWigApp.Application.DTOs;

namespace WinWigApp.Application.Validators;

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
