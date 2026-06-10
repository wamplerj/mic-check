using NUnit.Framework;
using MicCheck.Api.Data;

namespace MicCheck.Api.Tests.Unit.Data;

[TestFixture]
public class DatabaseUrlParserTests
{
    [Test]
    public void WhenAFullDatabaseUrlIsParsed_ThenAllComponentsAreExtracted()
    {
        var url = "postgresql://miccheck:password@localhost:5432/miccheck";

        var result = DatabaseUrlParser.ToNpgsqlConnectionString(url);

        Assert.That(result, Does.Contain("Host=localhost"));
        Assert.That(result, Does.Contain("Port=5432"));
        Assert.That(result, Does.Contain("Database=miccheck"));
        Assert.That(result, Does.Contain("Username=miccheck"));
        Assert.That(result, Does.Contain("Password=password"));
    }

    [Test]
    public void WhenADatabaseUrlWithNoExplicitPortIsParsed_ThenPortDefaultsTo5432()
    {
        var url = "postgresql://user:pass@db.example.com/mydb";

        var result = DatabaseUrlParser.ToNpgsqlConnectionString(url);

        Assert.That(result, Does.Contain("Port=5432"));
    }

    [Test]
    public void WhenADatabaseUrlWithACustomPortIsParsed_ThenThatPortIsUsed()
    {
        var url = "postgresql://user:pass@db.example.com:5433/mydb";

        var result = DatabaseUrlParser.ToNpgsqlConnectionString(url);

        Assert.That(result, Does.Contain("Port=5433"));
    }

    [Test]
    public void WhenADatabaseUrlContainsSpecialCharactersInPassword_ThenTheyArePreserved()
    {
        var url = "postgresql://user:s3cr3t@host:5432/db";

        var result = DatabaseUrlParser.ToNpgsqlConnectionString(url);

        Assert.That(result, Does.Contain("Password=s3cr3t"));
    }
}
