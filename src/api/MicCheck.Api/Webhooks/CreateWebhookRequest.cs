using MicCheck.Api.Common.Validation;

namespace MicCheck.Api.Webhooks;

public record CreateWebhookRequest(string Url, string? Secret, bool Enabled);

public class CreateWebhookRequestValidator : IModelValidator<CreateWebhookRequest>
{
    public ValidationResult Validate(CreateWebhookRequest model)
    {
        var result = new ValidationResult();

        if (string.IsNullOrEmpty(model.Url))
            result.AddError(nameof(model.Url), "'Url' must not be empty.");
        else if (model.Url.Length > 500)
            result.AddError(nameof(model.Url), "'Url' must be 500 characters or fewer.");
        else if (!Uri.TryCreate(model.Url, UriKind.Absolute, out _))
            result.AddError(nameof(model.Url), "Url must be a valid absolute URL.");

        return result;
    }
}
