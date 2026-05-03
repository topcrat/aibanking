using AIBanking.Models;

namespace AIBanking.Services;

public interface IUserService
{
    /// <summary>Validate credentials. Returns the user on success, null on failure.</summary>
    Task<User?> ValidateAsync(string username, string password, CancellationToken ct = default);

    /// <summary>Create a new user. Throws if username already exists.</summary>
    Task<User> CreateAsync(string username, string password, string fullName, string role, CancellationToken ct = default);

    /// <summary>List all users (passwords excluded).</summary>
    Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default);

    /// <summary>Change a user's password.</summary>
    Task<bool> ChangePasswordAsync(Guid userId, string newPassword, CancellationToken ct = default);

    /// <summary>Enable or disable a user account.</summary>
    Task<bool> SetActiveAsync(Guid userId, bool isActive, CancellationToken ct = default);

    /// <summary>Ensure at least one admin user exists; seed one if the table is empty.</summary>
    Task SeedAdminIfEmptyAsync(string defaultPassword, CancellationToken ct = default);
}
