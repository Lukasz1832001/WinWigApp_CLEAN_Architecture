using FluentValidation;
using WinWigApp.Server.DTOs;

namespace WinWigApp.Server.Validators;

public class DepositRequestValidator : AbstractValidator<DepositRequest>
{
    public DepositRequestValidator()
    {
        RuleFor(x => x.Amount)
            .NotEmpty()
            .WithMessage("Kwota jest wymagana")
            .GreaterThan(0)
            .WithMessage("Kwota musi być większa niż 0")
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Kwota nie może być większa niż 1 000 000");

        RuleFor(x => x.Method)
            .NotEmpty()
            .WithMessage("Metoda płatności jest wymagana")
            .Length(2, 50)
            .WithMessage("Metoda płatności musi mieć od 2 do 50 znaków");
    }
}
