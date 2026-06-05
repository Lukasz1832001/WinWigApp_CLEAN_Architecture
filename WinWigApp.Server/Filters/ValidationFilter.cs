using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WinWigApp.Server.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument == null)
                continue;

            var argumentType = argument.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);

            var validator = _serviceProvider.GetService(validatorType);
            if (validator != null)
            {
                var validationMethod = validatorType.GetMethod("ValidateAsync", new[] { argumentType });
                if (validationMethod != null)
                {
                    var result = await (dynamic)validationMethod.Invoke(validator, new[] { argument })!;
                    if (!result.IsValid)
                    {
                        var validationException = new ValidationException(result.Errors);
                        throw validationException;
                    }
                }
            }
        }

        await next();
    }
}
