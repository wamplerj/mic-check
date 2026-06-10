using NUnit.Framework;
using MicCheck.Api.Users;

namespace MicCheck.Api.Tests.Unit.Users;

[TestFixture]
public class UserTests
{
    [Test]
    public void WhenAUserIsCreated_ThenOrganizationsCollectionIsInitializedEmpty()
    {
        var user = new User
        {
            Email = "alice@example.com",
            PasswordHash = "hashed",
            FirstName = "Alice",
            LastName = "Smith"
        };

        Assert.That(user.Organizations, Is.Empty);
    }

    [Test]
    public void WhenAUserIsCreated_ThenIsActiveDefaultsToFalse()
    {
        var user = new User
        {
            Email = "alice@example.com",
            PasswordHash = "hashed",
            FirstName = "Alice",
            LastName = "Smith"
        };

        Assert.That(user.IsActive, Is.False);
    }

    [Test]
    public void WhenAUserIsCreated_ThenIsTwoFactorEnabledDefaultsToFalse()
    {
        var user = new User
        {
            Email = "alice@example.com",
            PasswordHash = "hashed",
            FirstName = "Alice",
            LastName = "Smith"
        };

        Assert.That(user.IsTwoFactorEnabled, Is.False);
    }

    [Test]
    public void WhenAUserIsCreated_ThenLastLoginAtDefaultsToNull()
    {
        var user = new User
        {
            Email = "alice@example.com",
            PasswordHash = "hashed",
            FirstName = "Alice",
            LastName = "Smith"
        };

        Assert.That(user.LastLoginAt, Is.Null);
    }
}
