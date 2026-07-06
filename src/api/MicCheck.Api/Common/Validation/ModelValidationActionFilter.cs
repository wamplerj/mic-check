using Microsoft.AspNetCore.Mvc.Filters;

namespace MicCheck.Api.Common.Validation;

public class ModelValidationActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IModelValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IModelValidator validator) continue;

            var result = validator.Validate(argument);
            foreach (var error in result.Errors)
                context.ModelState.AddModelError(error.PropertyName, error.Message);
        }

        if (!context.ModelState.IsValid)
            context.Result = ValidationProblemResponseFactory.Create(context.ModelState);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
