using System.Text.RegularExpressions;
using MicCheck.Api.Common.Validation;

namespace MicCheck.Api.Features;

public record CreateTagRequest(string Label, string Color);

public class CreateTagRequestValidator : IModelValidator<CreateTagRequest>
{
    private static readonly Regex ColorPattern = new("^#[0-9A-Fa-f]{3,6}$");

    public ValidationResult Validate(CreateTagRequest model)
    {
        var result = new ValidationResult();

        if (string.IsNullOrEmpty(model.Label))
            result.AddError(nameof(model.Label), "'Label' must not be empty.");
        else if (model.Label.Length > 100)
            result.AddError(nameof(model.Label), "'Label' must be 100 characters or fewer.");

        if (string.IsNullOrEmpty(model.Color))
            result.AddError(nameof(model.Color), "'Color' must not be empty.");
        else if (model.Color.Length > 20)
            result.AddError(nameof(model.Color), "'Color' must be 20 characters or fewer.");
        else if (!ColorPattern.IsMatch(model.Color))
            result.AddError(nameof(model.Color), "Color must be a valid hex color (e.g. #FF0000).");

        return result;
    }
}
