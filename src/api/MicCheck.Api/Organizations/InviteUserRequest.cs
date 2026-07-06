using MicCheck.Api.Common.Validation;

namespace MicCheck.Api.Organizations;

public record InviteUserRequest(int UserId, string Role);

public class InviteUserRequestValidator : IModelValidator<InviteUserRequest>
{
    public ValidationResult Validate(InviteUserRequest model)
    {
        var result = new ValidationResult();

        if (model.UserId <= 0)
            result.AddError(nameof(model.UserId), "'User Id' must be greater than 0.");

        if (string.IsNullOrEmpty(model.Role))
            result.AddError(nameof(model.Role), "'Role' must not be empty.");
        else if (!Enum.TryParse<OrganizationRole>(model.Role, true, out _))
            result.AddError(nameof(model.Role), "Role must be 'User' or 'Admin'.");

        return result;
    }
}
