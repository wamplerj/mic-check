using MicCheck.Api.Common.Validation;

namespace MicCheck.Api.Environments;

public record CreateEnvironmentRequest(string Name, int ProjectId);

public class CreateEnvironmentRequestValidator : IModelValidator<CreateEnvironmentRequest>
{
    public ValidationResult Validate(CreateEnvironmentRequest model)
    {
        var result = new ValidationResult();

        if (string.IsNullOrEmpty(model.Name))
            result.AddError(nameof(model.Name), "'Name' must not be empty.");
        else if (model.Name.Length > 200)
            result.AddError(nameof(model.Name), "'Name' must be 200 characters or fewer.");

        if (model.ProjectId <= 0)
            result.AddError(nameof(model.ProjectId), "'Project Id' must be greater than 0.");

        return result;
    }
}
