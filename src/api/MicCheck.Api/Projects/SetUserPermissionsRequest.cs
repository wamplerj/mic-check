using FluentValidation;
using MicCheck.Api.Common.Security.Authorization;

namespace MicCheck.Api.Projects;

public record SetUserPermissionsRequest(int UserId, bool IsAdmin, List<string> Permissions);

public class SetUserPermissionsRequestValidator : AbstractValidator<SetUserPermissionsRequest>
{
    public SetUserPermissionsRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleForEach(x => x.Permissions)
            .Must(p => Enum.TryParse<ProjectPermission>(p, true, out _))
            .WithMessage("Invalid permission value.");
    }
}
