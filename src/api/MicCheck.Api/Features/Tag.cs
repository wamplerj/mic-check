namespace MicCheck.Api.Features;

public class Tag
{
    public int Id { get; init; }
    public required string Label { get; set; }
    public required string Color { get; set; }
    public int ProjectId { get; init; }
}
