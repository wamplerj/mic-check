using FluentValidation;

namespace MicCheck.Api.Features;

public record UpdateFeatureRequest(
    string Name,
    string? Description
);

public class UpdateFeatureRequestValidator : AbstractValidator<UpdateFeatureRequest>
{
    public UpdateFeatureRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150)
            .Matches("^[a-zA-Z0-9_-]+$")
            .WithMessage("Name may only contain letters, digits, underscores, and hyphens.");
    }
}
