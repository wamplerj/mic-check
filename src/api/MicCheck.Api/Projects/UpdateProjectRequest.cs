using MicCheck.Api.Common.Validation;

namespace MicCheck.Api.Projects;

public record UpdateProjectRequest(string Name, bool HideDisabledFlags);

public class UpdateProjectRequestValidator : IModelValidator<UpdateProjectRequest>
{
    public ValidationResult Validate(UpdateProjectRequest model)
    {
        var result = new ValidationResult();

        if (string.IsNullOrEmpty(model.Name))
            result.AddError(nameof(model.Name), "'Name' must not be empty.");
        else if (model.Name.Length > 200)
            result.AddError(nameof(model.Name), "'Name' must be 200 characters or fewer.");

        return result;
    }
}
