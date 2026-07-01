using FluentValidation;

namespace MicCheck.Api.Organizations;

public record InviteUserRequest(int UserId, string Role);

public class InviteUserRequestValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Role).NotEmpty().Must(r => Enum.TryParse<OrganizationRole>(r, true, out _))
            .WithMessage("Role must be 'User' or 'Admin'.");
    }
}
