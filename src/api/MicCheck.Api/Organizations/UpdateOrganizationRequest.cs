using FluentValidation;

namespace MicCheck.Api.Organizations;

public record UpdateOrganizationRequest(string Name);

public class UpdateOrganizationRequestValidator : AbstractValidator<UpdateOrganizationRequest>
{
    public UpdateOrganizationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
