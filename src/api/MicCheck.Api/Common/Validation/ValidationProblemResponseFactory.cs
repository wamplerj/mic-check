using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MicCheck.Api.Common.Validation;

public static class ValidationProblemResponseFactory
{
    public static IActionResult Create(ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        return new UnprocessableEntityObjectResult(new { errors });
    }
}
