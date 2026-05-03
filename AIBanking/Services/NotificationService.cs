using AIBanking.Data;
using AIBanking.Enums;
using AIBanking.Models;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.Services;

/// <summary>
/// Logs notifications to the database and simulates delivery.
/// Replace the per-channel stubs with real providers (Twilio, SendGrid, FCM) in production.
/// </summary>
public sealed class NotificationService(
    IDbContextFactory<BankingDbContext> dbFactory,
    ILogger<NotificationService>        logger) : INotificationService
{
    public async Task SendAsync(Guid customerId, string subject, string message, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var prefs = await context.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CustomerId == customerId, ct);

        if (prefs is null)
        {
            logger.LogWarning("No notification preferences found for customer {Id}. Skipping.", customerId);
            return;
        }

        var tasks = new List<Task>();

        // SMS — mandatory
        if (prefs.SmsEnabled && !string.IsNullOrWhiteSpace(prefs.PhoneNumber))
            tasks.Add(SendAndLogAsync(context, customerId, NotificationType.Sms, prefs.PhoneNumber, subject, message, ct));

        // Email — optional
        if (prefs.EmailEnabled && !string.IsNullOrWhiteSpace(prefs.Email))
            tasks.Add(SendAndLogAsync(context, customerId, NotificationType.Email, prefs.Email, subject, message, ct));

        // Push — optional
        if (prefs.PushEnabled && !string.IsNullOrWhiteSpace(prefs.DeviceToken))
            tasks.Add(SendAndLogAsync(context, customerId, NotificationType.PushNotification, prefs.DeviceToken, subject, message, ct));

        await Task.WhenAll(tasks);
        await context.SaveChangesAsync(ct);
    }

    public async Task SendAsync(Guid customerId, NotificationType type, string subject, string message, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var prefs = await context.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CustomerId == customerId, ct);

        var recipient = type switch
        {
            NotificationType.Sms              => prefs?.PhoneNumber ?? string.Empty,
            NotificationType.Email            => prefs?.Email ?? string.Empty,
            NotificationType.PushNotification => prefs?.DeviceToken ?? string.Empty,
            _                                 => string.Empty
        };

        if (string.IsNullOrWhiteSpace(recipient))
        {
            logger.LogWarning("Cannot send {Type} to customer {Id} — recipient not configured.", type, customerId);
            return;
        }

        await SendAndLogAsync(context, customerId, type, recipient, subject, message, ct);
        await context.SaveChangesAsync(ct);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task SendAndLogAsync(
        BankingDbContext context,
        Guid             customerId,
        NotificationType type,
        string           recipient,
        string           subject,
        string           message,
        CancellationToken ct)
    {
        var log = new NotificationLog
        {
            Id         = Guid.NewGuid(),
            CustomerId = customerId,
            Type       = type,
            Recipient  = recipient,
            Subject    = subject,
            Message    = message,
            SentAt     = DateTime.UtcNow
        };

        try
        {
            // ── Provider stubs ──────────────────────────────────────────────
            // Replace each stub with the real provider SDK call:
            //   SMS  → Twilio, Africa's Talking, etc.
            //   Email → SendGrid, AWS SES, etc.
            //   Push  → Firebase FCM, etc.
            await Task.CompletedTask;   // simulate async dispatch

            log.Status = NotificationStatus.Sent;
            logger.LogInformation("[{Type}] to {Recipient}: {Subject}", type, MaskRecipient(recipient), subject);
        }
        catch (Exception ex)
        {
            log.Status       = NotificationStatus.Failed;
            log.ErrorMessage = ex.Message;
            logger.LogError(ex, "Failed to send {Type} to {Recipient}", type, MaskRecipient(recipient));
        }

        context.NotificationLogs.Add(log);
    }

    private static string MaskRecipient(string r) =>
        r.Length <= 4 ? "****" : string.Concat(r[..3], new string('*', r.Length - 3));
}
