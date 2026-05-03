using System.Security.Cryptography;
using System.Text;
using AIBanking.Data;
using AIBanking.Enums;
using AIBanking.Models;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.Services;

public sealed class DigitalEnrollmentService(
    IDbContextFactory<BankingDbContext> dbFactory,
    INotificationService                notifications,
    ILogger<DigitalEnrollmentService>   logger) : IDigitalEnrollmentService
{
    public async Task<DigitalEnrollment> EnrollAsync(
        Guid               customerId,
        Guid               accountId,
        DigitalServiceType serviceType,
        string             fullName,
        string             accountNumber,
        CancellationToken  ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        // Idempotent — skip if already enrolled
        var existing = await context.DigitalEnrollments
            .FirstOrDefaultAsync(e => e.CustomerId == customerId &&
                                      e.ServiceType == serviceType, ct);
        if (existing is not null)
        {
            logger.LogInformation("{Service} already enrolled for customer {Id}.", serviceType, customerId);
            return existing;
        }

        var username  = GenerateUsername(fullName, accountNumber);
        var tempPass  = GenerateTemporaryPassword();
        var passHash  = HashPassword(tempPass);

        var enrollment = new DigitalEnrollment
        {
            Id                  = Guid.NewGuid(),
            CustomerId          = customerId,
            AccountId           = accountId,
            ServiceType         = serviceType,
            Status              = DigitalEnrollmentStatus.Active,
            Username            = username,
            PasswordHash        = passHash,
            MustChangePassword  = true,
            EnrolledAt          = DateTime.UtcNow
        };

        context.DigitalEnrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);

        // Deliver credentials via SMS (secure channel — do NOT send full password via email)
        var serviceName = serviceType == DigitalServiceType.MobileBanking
            ? "Mobile Banking"
            : "Internet Banking";

        await notifications.SendAsync(customerId, NotificationType.Sms,
            $"Your {serviceName} credentials",
            $"Welcome! Your {serviceName} username is: {username}. " +
            $"Temporary password: {tempPass}. " +
            $"Please change your password on first login.");

        logger.LogInformation("{Service} enrollment completed for customer {Id}. Username: {User}",
            serviceType, customerId, username);

        return enrollment;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string GenerateUsername(string fullName, string accountNumber)
    {
        var namePart    = new string(fullName.Where(char.IsLetter).Take(4).ToArray()).ToUpper();
        var accountPart = accountNumber.Length >= 6
            ? accountNumber[^6..]
            : accountNumber;
        return $"{namePart}{accountPart}";
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789@#!";
        return new string(Enumerable.Range(0, 10)
            .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])
            .ToArray());
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
