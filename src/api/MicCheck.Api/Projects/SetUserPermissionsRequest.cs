using MicCheck.Api.Common.Security.Authorization;
using MicCheck.Api.Common.Validation;

namespace MicCheck.Api.Projects;

public record SetUserPermissionsRequest(int UserId, bool IsAdmin, List<string> Permissions);

public class SetUserPermissionsRequestValidator : IModelValidator<SetUserPermissionsRequest>
{
    public ValidationResult Validate(SetUserPermissionsRequest model)
    {
        var result = new ValidationResult();

        if (model.UserId <= 0)
            result.AddError(nameof(model.UserId), "'User Id' must be greater than 0.");

        foreach (var permission in model.Permissions)
        {
            if (!Enum.TryParse<ProjectPermission>(permission, true, out _))
                result.AddError(nameof(model.Permissions), "Invalid permission value.");
        }

        return result;
    }
}
