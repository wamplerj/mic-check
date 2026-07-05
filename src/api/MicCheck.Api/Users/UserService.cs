using MicCheck.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MicCheck.Api.Users;

public class UserService(IMicCheckDbContext db, IPasswordHasher<User> passwordHasher)
{
    public async Task<User?> FindByIdAsync(int userId, CancellationToken ct = default) =>
        await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);

    public async Task<ProfileUpdateResult> UpdateProfileAsync(
        int userId, string firstName, string lastName, string email, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);
        if (user is null) return new ProfileUpdateResult(null, EmailConflict: false);

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var emailTaken = await db.Users.AnyAsync(u => u.Email == email && u.Id != userId, ct);
            if (emailTaken) return new ProfileUpdateResult(null, EmailConflict: true);
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Email = email;
        await db.SaveChangesAsync(ct);
        return new ProfileUpdateResult(user, EmailConflict: false);
    }

    public async Task<bool> ChangePasswordAsync(
        int userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);
        if (user is null) return false;

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        if (result == PasswordVerificationResult.Failed) return false;

        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record ProfileUpdateResult(User? User, bool EmailConflict);
