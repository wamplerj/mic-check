namespace MicCheck.Api.Features;

public record Tag
{
    public int Id { get; init; }
    public required string Label { get; init; }
    public required string Color { get; init; }
    public int ProjectId { get; init; }
}
