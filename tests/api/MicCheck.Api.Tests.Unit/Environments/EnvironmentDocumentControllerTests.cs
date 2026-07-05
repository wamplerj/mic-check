using System.Security.Claims;
using MicCheck.Api.Data;
using MicCheck.Api.Environments;
using MicCheck.Api.Organizations;
using MicCheck.Api.Projects;
using MicCheck.Api.Tests.Unit.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Environments;

[TestFixture]
public class EnvironmentDocumentControllerTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private List<AppEnvironment> _environments = null!;
    private EnvironmentDocumentController _controller = null!;
    private const int EnvironmentId = 1;
    private const int ProjectId = 1;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();

        _environments = [new AppEnvironment { Id = EnvironmentId, Name = "Production", ApiKey = "env-key", ProjectId = ProjectId, CreatedAt = DateTimeOffset.UtcNow }];
        var environmentsSet = MockDbSetFactory.Create(_environments);
        environmentsSet.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object[] keys, CancellationToken _) => _environments.FirstOrDefault(e => e.Id == (int)keys[0]));
        _db.Setup(c => c.Environments).Returns(environmentsSet.Object);

        _db.SetupDbSet(c => c.Projects, [new Project { Id = ProjectId, Name = "Test Project", OrganizationId = 1, CreatedAt = DateTimeOffset.UtcNow }]);
        _db.SetupDbSet(c => c.Organizations, [new Organization { Id = 1, Name = "Org", CreatedAt = DateTimeOffset.UtcNow }]);
        _db.SetupDbSet(c => c.Features, []);
        _db.SetupDbSet(c => c.FeatureStates, []);

        _controller = new EnvironmentDocumentController(new EnvironmentDocumentService(_db.Object));

        var identity = new ClaimsIdentity([new Claim("EnvironmentId", EnvironmentId.ToString())], "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Test]
    public async Task WhenTheEnvironmentDocumentExists_ThenItIsReturned()
    {
        var result = await _controller.Get(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var document = ok!.Value as EnvironmentDocumentResponse;
        Assert.That(document!.Id, Is.EqualTo(EnvironmentId));
    }

    [Test]
    public async Task WhenTheEnvironmentDocumentDoesNotExist_ThenNotFoundIsReturned()
    {
        _environments.Clear();

        var result = await _controller.Get(CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }
}
