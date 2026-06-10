using FluentValidation;

namespace MicCheck.Api.Environments;

public record CreateEnvironmentRequest(string Name, int ProjectId);

public class CreateEnvironmentRequestValidator : AbstractValidator<CreateEnvironmentRequest>
{
    public CreateEnvironmentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ProjectId).GreaterThan(0);
    }
}
