using System.ComponentModel;
using System.Text.Json;
using AIBanking.Data;
using AIBanking.Enums;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.AIAgent.Agents.Tools;

/// <summary>Read-only tools for querying card requests and notification history.</summary>
internal sealed class CardTools(BankingDbContext context)
{
    [Description("Get the debit card request details for a specific bank account, including card type, delivery method, status, and PIN status.")]
    public async Task<string> GetCardRequestAsync(
        [Description("The bank account ID (GUID string)")] string accountId)
    {
        if (!Guid.TryParse(accountId, out var id))
            return Err("Invalid account ID format.");

        var card = await context.CardRequests.AsNoTracking().FirstOrDefaultAsync(c => c.AccountId == id);
        if (card is null) return Err($"No card request found for account {accountId}.");

        return JsonSerializer.Serialize(new
        {
            id             = card.Id,
            accountId      = card.AccountId,
            customerId     = card.CustomerId,
            cardType       = card.CardType.ToString(),
            deliveryMethod = card.DeliveryMethod.ToString(),
            branchCode     = card.BranchCode,
            deliveryAddress= card.DeliveryAddress,
            status         = card.Status.ToString(),
            trackingNumber = card.TrackingNumber,
            pinGenerated   = card.PinGenerated,
            requestedAt    = card.RequestedAt,
            dispatchedAt   = card.DispatchedAt,
            deliveredAt    = card.DeliveredAt
        });
    }

    [Description("List all card requests. Optionally filter by status: Pending, Processing, Dispatched, Delivered, Cancelled.")]
    public async Task<string> GetCardRequestsAsync(
        [Description("Optional status filter (Pending, Processing, Dispatched, Delivered, Cancelled)")] string? status = null)
    {
        var query = context.CardRequests.AsNoTracking().AsQueryable();

        if (status is not null && Enum.TryParse<CardRequestStatus>(status, ignoreCase: true, out var parsed))
            query = query.Where(c => c.Status == parsed);

        var cards = await query.OrderByDescending(c => c.RequestedAt).ToListAsync();

        return JsonSerializer.Serialize(cards.Select(c => new
        {
            id             = c.Id,
            accountId      = c.AccountId,
            customerId     = c.CustomerId,
            cardType       = c.CardType.ToString(),
            deliveryMethod = c.DeliveryMethod.ToString(),
            status         = c.Status.ToString(),
            pinGenerated   = c.PinGenerated,
            requestedAt    = c.RequestedAt
        }));
    }

    [Description("Get notification history for a customer. Returns recent SMS, email, and push notifications sent.")]
    public async Task<string> GetNotificationHistoryAsync(
        [Description("The customer ID (GUID string)")] string customerId,
        [Description("Max number of records to return (default 20)")] int limit = 20)
    {
        if (!Guid.TryParse(customerId, out var id))
            return Err("Invalid customer ID format.");

        var logs = await context.NotificationLogs
            .AsNoTracking()
            .Where(n => n.CustomerId == id)
            .OrderByDescending(n => n.SentAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync();

        return JsonSerializer.Serialize(logs.Select(n => new
        {
            id        = n.Id,
            type      = n.Type.ToString(),
            status    = n.Status.ToString(),
            subject   = n.Subject,
            sentAt    = n.SentAt
        }));
    }

    [Description("Get the notification preferences for a customer (SMS, email, push settings).")]
    public async Task<string> GetNotificationPreferencesAsync(
        [Description("The customer ID (GUID string)")] string customerId)
    {
        if (!Guid.TryParse(customerId, out var id))
            return Err("Invalid customer ID format.");

        var prefs = await context.NotificationPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.CustomerId == id);
        if (prefs is null) return Err($"No notification preferences found for customer {customerId}.");

        return JsonSerializer.Serialize(new
        {
            customerId   = prefs.CustomerId,
            smsEnabled   = prefs.SmsEnabled,
            phoneNumber  = prefs.PhoneNumber,
            emailEnabled = prefs.EmailEnabled,
            email        = prefs.Email,
            pushEnabled  = prefs.PushEnabled
        });
    }

    private static string Err(string message) => JsonSerializer.Serialize(new { error = message });
}
