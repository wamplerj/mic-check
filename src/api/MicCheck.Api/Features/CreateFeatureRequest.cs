using FluentValidation;

namespace MicCheck.Api.Features;

public record CreateFeatureRequest(
    string Name,
    FeatureType Type,
    string? InitialValue,
    string? Description
);

public class CreateFeatureRequestValidator : AbstractValidator<CreateFeatureRequest>
{
    public CreateFeatureRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150)
            .Matches("^[a-zA-Z0-9_-]+$")
            .WithMessage("Name may only contain letters, digits, underscores, and hyphens.");

        RuleFor(x => x.InitialValue)
            .MaximumLength(20_000)
            .When(x => x.InitialValue is not null);
    }
}
