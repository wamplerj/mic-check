using FluentValidation;

namespace MicCheck.Api.Features;

public record CreateTagRequest(string Label, string Color);

public class CreateTagRequestValidator : AbstractValidator<CreateTagRequest>
{
    public CreateTagRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(20)
            .Matches("^#[0-9A-Fa-f]{3,6}$")
            .WithMessage("Color must be a valid hex color (e.g. #FF0000).");
    }
}
