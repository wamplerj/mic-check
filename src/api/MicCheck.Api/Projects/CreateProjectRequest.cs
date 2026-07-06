using MicCheck.Api.Common.Validation;

namespace MicCheck.Api.Projects;

public record CreateProjectRequest(string Name, int OrganizationId);

public class CreateProjectRequestValidator : IModelValidator<CreateProjectRequest>
{
    public ValidationResult Validate(CreateProjectRequest model)
    {
        var result = new ValidationResult();

        if (string.IsNullOrEmpty(model.Name))
            result.AddError(nameof(model.Name), "'Name' must not be empty.");
        else if (model.Name.Length > 200)
            result.AddError(nameof(model.Name), "'Name' must be 200 characters or fewer.");

        if (model.OrganizationId <= 0)
            result.AddError(nameof(model.OrganizationId), "'Organization Id' must be greater than 0.");

        return result;
    }
}
