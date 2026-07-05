using MicCheck.Api.Data;
using MicCheck.Api.Organizations;
using MicCheck.Api.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Data;

[TestFixture]
public class DatabaseSeederTests
{
    private MicCheckDbContext _db = null!;
    private DatabaseSeeder _seeder = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<MicCheckDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new MicCheckDbContext(options);
        _seeder = new DatabaseSeeder(_db, new PasswordHasher<User>());
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task WhenTheDatabaseIsEmpty_ThenSeedingCreatesADeterministicAdminUser()
    {
        await _seeder.SeedAsync();

        var admin = await _db.Users.SingleOrDefaultAsync(u => u.Email == DatabaseSeeder.SeedAdminEmail);

        Assert.That(admin, Is.Not.Null);
        Assert.That(admin!.IsActive, Is.True);
    }

    [Test]
    public async Task WhenTheDatabaseIsEmpty_ThenTheSeededAdminPasswordVerifiesAgainstTheKnownCredential()
    {
        await _seeder.SeedAsync();

        var admin = await _db.Users.SingleAsync(u => u.Email == DatabaseSeeder.SeedAdminEmail);
        var hasher = new PasswordHasher<User>();

        var result = hasher.VerifyHashedPassword(admin, admin.PasswordHash, DatabaseSeeder.SeedAdminPassword);

        Assert.That(result, Is.Not.EqualTo(PasswordVerificationResult.Failed));
    }

    [Test]
    public async Task WhenTheDatabaseIsEmpty_ThenTheSeededAdminIsAnAdminOfTheDefaultOrganization()
    {
        await _seeder.SeedAsync();

        var organization = await _db.Organizations.SingleAsync(o => o.Name == "Default");
        var admin = await _db.Users.SingleAsync(u => u.Email == DatabaseSeeder.SeedAdminEmail);
        var membership = await _db.OrganizationUsers
            .SingleAsync(ou => ou.OrganizationId == organization.Id && ou.UserId == admin.Id);

        Assert.That(membership.Role, Is.EqualTo(OrganizationRole.Admin));
        Assert.That(membership.IsPrimary, Is.True);
    }

    [Test]
    public async Task WhenTheDatabaseIsEmpty_ThenTheSeededDevelopmentEnvironmentHasTheFixedApiKey()
    {
        await _seeder.SeedAsync();

        var development = await _db.Environments.SingleAsync(e => e.Name == "Development");
        var staging = await _db.Environments.SingleAsync(e => e.Name == "Staging");
        var production = await _db.Environments.SingleAsync(e => e.Name == "Production");

        Assert.That(development.ApiKey, Is.EqualTo(DatabaseSeeder.SeedDevelopmentEnvironmentKey));
        Assert.That(staging.ApiKey, Is.Not.EqualTo(DatabaseSeeder.SeedDevelopmentEnvironmentKey));
        Assert.That(production.ApiKey, Is.Not.EqualTo(DatabaseSeeder.SeedDevelopmentEnvironmentKey));
    }

    [Test]
    public async Task WhenSeedingRunsTwice_ThenItDoesNotCreateDuplicateOrganizationsOrUsers()
    {
        await _seeder.SeedAsync();
        await _seeder.SeedAsync();

        Assert.That(await _db.Organizations.CountAsync(), Is.EqualTo(1));
        Assert.That(await _db.Users.CountAsync(u => u.Email == DatabaseSeeder.SeedAdminEmail), Is.EqualTo(1));
        Assert.That(await _db.Environments.CountAsync(), Is.EqualTo(3));
    }
}
