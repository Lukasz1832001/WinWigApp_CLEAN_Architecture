using FluentValidation;
using WinWigApp.Application.DTOs;

namespace WinWigApp.Application.Validators;

public class UpdateStopLossRequestValidator : AbstractValidator<UpdateStopLossRequest>
{
    public UpdateStopLossRequestValidator()
    {
        RuleFor(x => x.StopLoss)
            .NotEmpty()
            .WithMessage("Stop Loss jest wymagany")
            .GreaterThan(0)
            .WithMessage("Stop Loss musi być większy niż 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Stop Loss nie może być większy niż 100%");
    }
}
