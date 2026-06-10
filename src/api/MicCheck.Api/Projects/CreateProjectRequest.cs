using FluentValidation;

namespace MicCheck.Api.Projects;

public record CreateProjectRequest(string Name, int OrganizationId);

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OrganizationId).GreaterThan(0);
    }
}
