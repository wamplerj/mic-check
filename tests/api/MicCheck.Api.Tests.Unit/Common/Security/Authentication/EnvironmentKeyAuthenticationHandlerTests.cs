using System.Text.Encodings.Web;
using MicCheck.Api.Common.Security.Authentication;
using MicCheck.Api.Data;
using MicCheck.Api.Tests.Unit.TestSupport;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using AppEnvironment = MicCheck.Api.Environments.Environment;

namespace MicCheck.Api.Tests.Unit.Common.Security.Authentication;

[TestFixture]
public class EnvironmentKeyAuthenticationHandlerTests
{
    private List<AppEnvironment> _environments = null!;
    private Mock<IMicCheckDbContext> _db = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();
        _environments = [];
        _db.SetupDbSet(c => c.Environments, _environments);
    }

    private async Task<AuthenticateResult> AuthenticateAsync(string? environmentKeyHeader)
    {
        var optionsMonitor = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        optionsMonitor.Setup(o => o.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());

        var handler = new EnvironmentKeyAuthenticationHandler(optionsMonitor.Object, NullLoggerFactory.Instance, UrlEncoder.Default, _db.Object);

        var httpContext = new DefaultHttpContext();
        if (environmentKeyHeader is not null)
            httpContext.Request.Headers["X-Environment-Key"] = environmentKeyHeader;

        var scheme = new AuthenticationScheme(EnvironmentKeyAuthenticationHandler.SchemeName, null, typeof(EnvironmentKeyAuthenticationHandler));
        await handler.InitializeAsync(scheme, httpContext);

        return await handler.AuthenticateAsync();
    }

    [Test]
    public async Task WhenEnvironmentKeyHeaderIsAbsent_ThenNoResultIsReturned()
    {
        var result = await AuthenticateAsync(null);

        Assert.That(result.None, Is.True);
    }

    [Test]
    public async Task WhenEnvironmentKeyIsInvalid_ThenAuthenticationFails()
    {
        var result = await AuthenticateAsync("not-a-real-key");

        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public async Task WhenEnvironmentKeyIsValid_ThenAuthenticationSucceedsWithEnvironmentClaims()
    {
        _environments.Add(new AppEnvironment { Id = 7, Name = "Prod", ApiKey = "valid-env-key", ProjectId = 3, CreatedAt = DateTimeOffset.UtcNow });

        var result = await AuthenticateAsync("valid-env-key");

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Principal!.FindFirst("EnvironmentId")!.Value, Is.EqualTo("7"));
        Assert.That(result.Principal!.FindFirst("ProjectId")!.Value, Is.EqualTo("3"));
    }
}
