using System.Security.Cryptography;
using System.Text;
using AIBanking.Data;
using AIBanking.Models;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.Services;

public sealed class UserService(
    IDbContextFactory<BankingDbContext> dbFactory,
    ILogger<UserService>                logger) : IUserService
{
    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<User?> ValidateAsync(string username, string password, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == username.Trim().ToLower() && u.IsActive, ct);

        if (user is null || !VerifyPassword(password, user.PasswordHash))
        {
            logger.LogWarning("Failed login attempt for username '{Username}'.", username);
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("User '{Username}' logged in.", username);
        return user;
    }

    public async Task<User> CreateAsync(
        string username, string password, string fullName, string role,
        CancellationToken ct = default)
    {
        if (!UserRoles.All.Contains(role))
            throw new ArgumentException($"Invalid role '{role}'. Valid: {string.Join(", ", UserRoles.All)}");

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var normalised = username.Trim().ToLower();
        if (await db.Users.AnyAsync(u => u.Username == normalised, ct))
            throw new InvalidOperationException($"Username '{username}' is already taken.");

        var user = new User
        {
            Id           = Guid.NewGuid(),
            Username     = normalised,
            PasswordHash = HashPassword(password),
            FullName     = fullName.Trim(),
            Role         = role,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("User '{Username}' ({Role}) created.", user.Username, user.Role);
        return user;
    }

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Users.AsNoTracking().OrderBy(u => u.Username).ToListAsync(ct);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string newPassword, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return false;

        user.PasswordHash = HashPassword(newPassword);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Password changed for user '{Username}'.", user.Username);
        return true;
    }

    public async Task<bool> SetActiveAsync(Guid userId, bool isActive, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return false;

        user.IsActive = isActive;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("User '{Username}' IsActive → {State}.", user.Username, isActive);
        return true;
    }

    public async Task SeedAdminIfEmptyAsync(string defaultPassword, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (await db.Users.AnyAsync(ct)) return;

        var admin = new User
        {
            Id           = Guid.NewGuid(),
            Username     = "admin",
            PasswordHash = HashPassword(defaultPassword),
            FullName     = "System Administrator",
            Role         = UserRoles.Admin,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync(ct);

        logger.LogWarning(
            "Admin user seeded with default password. CHANGE THIS IMMEDIATELY via POST /api/auth/users/{{id}}/password.");
    }

    // ── Password hashing (PBKDF2 / SHA-256, 100k iterations) ─────────────────

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations:  100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32);

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;

        byte[] salt, expected;
        try
        {
            salt     = Convert.FromBase64String(parts[0]);
            expected = Convert.FromBase64String(parts[1]);
        }
        catch { return false; }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations:    100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength:  32);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
