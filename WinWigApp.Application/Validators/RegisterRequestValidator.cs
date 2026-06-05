using FluentValidation;
using WinWigApp.Application.DTOs;

namespace WinWigApp.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("Imię jest wymagane")
            .MaximumLength(100)
            .WithMessage("Imię nie może być dłuższe niż 100 znaków");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Nazwisko jest wymagane")
            .MaximumLength(100)
            .WithMessage("Nazwisko nie może być dłuższe niż 100 znaków");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email jest wymagany")
            .EmailAddress()
            .WithMessage("Email musi być prawidłowy");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Hasło jest wymagane")
            .MinimumLength(8)
            .WithMessage("Hasło musi mieć minimum 8 znaków");
    }
}
