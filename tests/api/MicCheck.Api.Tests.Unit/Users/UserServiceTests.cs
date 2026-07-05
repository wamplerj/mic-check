using MicCheck.Api.Data;
using MicCheck.Api.Tests.Unit.TestSupport;
using MicCheck.Api.Users;
using Microsoft.AspNetCore.Identity;
using Moq;
using NUnit.Framework;

namespace MicCheck.Api.Tests.Unit.Users;

[TestFixture]
public class UserServiceTests
{
    private Mock<IMicCheckDbContext> _db = null!;
    private Mock<IPasswordHasher<User>> _passwordHasher = null!;
    private List<User> _users = null!;
    private UserService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IMicCheckDbContext>();
        _users = [];
        _db.SetupDbSet(c => c.Users, _users);

        _passwordHasher = new Mock<IPasswordHasher<User>>();

        _service = new UserService(_db.Object, _passwordHasher.Object);
    }

    private User AddUser(int id = 1, bool isActive = true, string email = "alice@example.com") => new()
    {
        Id = id,
        Email = email,
        PasswordHash = "hashed",
        FirstName = "Alice",
        LastName = "Smith",
        IsActive = isActive,
        CreatedAt = DateTimeOffset.UtcNow
    };

    [Test]
    public async Task WhenFindingByIdForAnActiveUser_ThenTheUserIsReturned()
    {
        var user = AddUser();
        _users.Add(user);

        var result = await _service.FindByIdAsync(user.Id);

        Assert.That(result, Is.SameAs(user));
    }

    [Test]
    public async Task WhenFindingByIdForAnInactiveUser_ThenNullIsReturned()
    {
        var user = AddUser(isActive: false);
        _users.Add(user);

        var result = await _service.FindByIdAsync(user.Id);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task WhenFindingByIdForAnUnknownUser_ThenNullIsReturned()
    {
        var result = await _service.FindByIdAsync(999);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task WhenUpdatingTheProfileOfAnUnknownUser_ThenNullIsReturnedWithoutConflict()
    {
        var result = await _service.UpdateProfileAsync(999, "Bob", "Jones", "bob@example.com");

        Assert.That(result.User, Is.Null);
        Assert.That(result.EmailConflict, Is.False);
    }

    [Test]
    public async Task WhenUpdatingTheProfileWithANewUniqueEmail_ThenTheUserFieldsAreUpdated()
    {
        var user = AddUser();
        _users.Add(user);

        var result = await _service.UpdateProfileAsync(user.Id, "Bob", "Jones", "bob@example.com");

        Assert.That(result.EmailConflict, Is.False);
        Assert.That(result.User!.FirstName, Is.EqualTo("Bob"));
        Assert.That(result.User.LastName, Is.EqualTo("Jones"));
        Assert.That(result.User.Email, Is.EqualTo("bob@example.com"));
    }

    [Test]
    public async Task WhenUpdatingTheProfileWithTheSameEmailInDifferentCase_ThenNoConflictIsDetected()
    {
        var user = AddUser(email: "alice@example.com");
        _users.Add(user);

        var result = await _service.UpdateProfileAsync(user.Id, "Alice", "Smith", "ALICE@EXAMPLE.COM");

        Assert.That(result.EmailConflict, Is.False);
        Assert.That(result.User!.Email, Is.EqualTo("ALICE@EXAMPLE.COM"));
    }

    [Test]
    public async Task WhenUpdatingTheProfileWithAnEmailAlreadyUsedByAnotherUser_ThenAConflictIsReturnedAndTheUserIsUnchanged()
    {
        var user = AddUser(id: 1, email: "alice@example.com");
        var otherUser = AddUser(id: 2, email: "taken@example.com");
        _users.Add(user);
        _users.Add(otherUser);

        var result = await _service.UpdateProfileAsync(user.Id, "Bob", "Jones", "taken@example.com");

        Assert.That(result.EmailConflict, Is.True);
        Assert.That(result.User, Is.Null);
        Assert.That(user.Email, Is.EqualTo("alice@example.com"));
    }

    [Test]
    public async Task WhenChangingThePasswordForAnUnknownUser_ThenFalseIsReturned()
    {
        var result = await _service.ChangePasswordAsync(999, "current", "new");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task WhenChangingThePasswordWithAnIncorrectCurrentPassword_ThenFalseIsReturnedAndTheHashIsUnchanged()
    {
        var user = AddUser();
        _users.Add(user);
        _passwordHasher
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "wrong"))
            .Returns(PasswordVerificationResult.Failed);

        var result = await _service.ChangePasswordAsync(user.Id, "wrong", "new-password");

        Assert.That(result, Is.False);
        Assert.That(user.PasswordHash, Is.EqualTo("hashed"));
    }

    [Test]
    public async Task WhenChangingThePasswordWithACorrectCurrentPassword_ThenTheHashIsUpdatedAndTrueIsReturned()
    {
        var user = AddUser();
        _users.Add(user);
        _passwordHasher
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, "current"))
            .Returns(PasswordVerificationResult.Success);
        _passwordHasher
            .Setup(h => h.HashPassword(user, "new-password"))
            .Returns("new-hashed");

        var result = await _service.ChangePasswordAsync(user.Id, "current", "new-password");

        Assert.That(result, Is.True);
        Assert.That(user.PasswordHash, Is.EqualTo("new-hashed"));
    }
}
