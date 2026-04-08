using FluentValidation;

namespace MicCheck.Api.Webhooks;

public record CreateWebhookRequest(string Url, string? Secret, bool Enabled);

public class CreateWebhookRequestValidator : AbstractValidator<CreateWebhookRequest>
{
    public CreateWebhookRequestValidator()
    {
        RuleFor(x => x.Url).NotEmpty().MaximumLength(500).Must(u => Uri.TryCreate(u, UriKind.Absolute, out _))
            .WithMessage("Url must be a valid absolute URL.");
    }
}
