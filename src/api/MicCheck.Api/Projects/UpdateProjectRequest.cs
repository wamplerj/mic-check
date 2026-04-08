using FluentValidation;

namespace MicCheck.Api.Projects;

public record UpdateProjectRequest(string Name, bool HideDisabledFlags);

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
