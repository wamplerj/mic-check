namespace MicCheck.Api.Segments;

public class Segment
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public int ProjectId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public ICollection<SegmentRule> Rules { get; init; } = [];
}
