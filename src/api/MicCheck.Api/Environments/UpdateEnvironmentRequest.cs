using FluentValidation;

namespace MicCheck.Api.Environments;

public record UpdateEnvironmentRequest(string Name);

public class UpdateEnvironmentRequestValidator : AbstractValidator<UpdateEnvironmentRequest>
{
    public UpdateEnvironmentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
