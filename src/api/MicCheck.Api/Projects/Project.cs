using MicCheck.Api.Features;
using MicCheck.Api.Segments;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Projects;

public class Project
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public int OrganizationId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public bool HideDisabledFlags { get; set; }
    public ICollection<AppEnvironment> Environments { get; init; } = [];
    public ICollection<Feature> Features { get; init; } = [];
    public ICollection<Segment> Segments { get; init; } = [];
}
