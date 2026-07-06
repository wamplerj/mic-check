using System.Text.RegularExpressions;
using MicCheck.Api.Common.Validation;

namespace MicCheck.Api.Features;

public record UpdateFeatureRequest(string Name, string? Description);

public class UpdateFeatureRequestValidator : IModelValidator<UpdateFeatureRequest>
{
    private static readonly Regex NamePattern = new("^[a-zA-Z0-9_-]+$");

    public ValidationResult Validate(UpdateFeatureRequest model)
    {
        var result = new ValidationResult();

        if (string.IsNullOrEmpty(model.Name))
            result.AddError(nameof(model.Name), "'Name' must not be empty.");
        else if (model.Name.Length > 150)
            result.AddError(nameof(model.Name), "'Name' must be 150 characters or fewer.");
        else if (!NamePattern.IsMatch(model.Name))
            result.AddError(nameof(model.Name), "Name may only contain letters, digits, underscores, and hyphens.");

        return result;
    }
}
