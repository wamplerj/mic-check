namespace MicCheck.Api.Features;

public record TagResponse(int Id, string Label, string Color, int ProjectId)
{
    public static TagResponse From(Tag tag) => new(tag.Id, tag.Label, tag.Color, tag.ProjectId);
}
