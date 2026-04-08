using FluentValidation;

namespace MicCheck.Api.Organizations;

public record CreateOrganizationRequest(string Name);

public class CreateOrganizationRequestValidator : AbstractValidator<CreateOrganizationRequest>
{
    public CreateOrganizationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
