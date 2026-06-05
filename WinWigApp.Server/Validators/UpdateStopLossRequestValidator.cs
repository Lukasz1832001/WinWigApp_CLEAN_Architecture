using FluentValidation;
using WinWigApp.Server.DTOs;

namespace WinWigApp.Server.Validators;

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
