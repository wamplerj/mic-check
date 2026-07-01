using FluentValidation;

namespace MicCheck.Api.Environments;

public record CloneEnvironmentRequest(string Name);

public class CloneEnvironmentRequestValidator : AbstractValidator<CloneEnvironmentRequest>
{
    public CloneEnvironmentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
