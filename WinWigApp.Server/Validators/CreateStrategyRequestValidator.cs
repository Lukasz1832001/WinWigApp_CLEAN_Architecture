using FluentValidation;
using WinWigApp.Server.DTOs;

namespace WinWigApp.Server.Validators;

public class CreateStrategyRequestValidator : AbstractValidator<CreateStrategyRequest>
{
    public CreateStrategyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Nazwa strategii jest wymagana")
            .MaximumLength(100)
            .WithMessage("Nazwa strategii nie może być dłuższa niż 100 znaków");

        RuleFor(x => x.TargetReturn)
            .NotEmpty()
            .WithMessage("Docelowy zwrot jest wymagany")
            .GreaterThan(0)
            .WithMessage("Docelowy zwrot musi być większy niż 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("Docelowy zwrot nie może być większy niż 1000%");

        RuleFor(x => x.InvestmentHorizon)
            .NotEmpty()
            .WithMessage("Horyzont inwestycyjny jest wymagany")
            .GreaterThan(0)
            .WithMessage("Horyzont inwestycyjny musi być większy niż 0")
            .LessThanOrEqualTo(3650)
            .WithMessage("Horyzont inwestycyjny nie może być większy niż 3650 dni (10 lat)");

        RuleFor(x => x.RsiLow)
            .GreaterThanOrEqualTo(0)
            .WithMessage("RSI Low musi być >= 0")
            .LessThanOrEqualTo(100)
            .WithMessage("RSI Low musi być <= 100");

        RuleFor(x => x.RsiHigh)
            .GreaterThanOrEqualTo(0)
            .WithMessage("RSI High musi być >= 0")
            .LessThanOrEqualTo(100)
            .WithMessage("RSI High musi być <= 100");

        RuleFor(x => x.RsiLow)
            .LessThan(x => x.RsiHigh)
            .WithMessage("RSI Low musi być mniejszy niż RSI High")
            .When(x => x.RsiHigh > 0);
    }
}
