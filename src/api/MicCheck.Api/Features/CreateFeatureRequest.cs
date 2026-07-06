using System.Text.RegularExpressions;
using MicCheck.Api.Common.Validation;

namespace MicCheck.Api.Features;

public record CreateFeatureRequest(string Name, FeatureType Type, string? InitialValue, string? Description);

public class CreateFeatureRequestValidator : IModelValidator<CreateFeatureRequest>
{
    private static readonly Regex NamePattern = new("^[a-zA-Z0-9_-]+$");

    public ValidationResult Validate(CreateFeatureRequest model)
    {
        var result = new ValidationResult();

        if (string.IsNullOrEmpty(model.Name))
            result.AddError(nameof(model.Name), "'Name' must not be empty.");
        else if (model.Name.Length > 150)
            result.AddError(nameof(model.Name), "'Name' must be 150 characters or fewer.");
        else if (!NamePattern.IsMatch(model.Name))
            result.AddError(nameof(model.Name), "Name may only contain letters, digits, underscores, and hyphens.");

        if (model.InitialValue is not null && model.InitialValue.Length > 20_000)
            result.AddError(nameof(model.InitialValue), "'Initial Value' must be 20000 characters or fewer.");

        return result;
    }
}
