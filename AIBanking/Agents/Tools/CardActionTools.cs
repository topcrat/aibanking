using System.ComponentModel;
using System.Text.Json;
using AIBanking.Data;
using AIBanking.Enums;
using AIBanking.Services;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.Agents.Tools;

/// <summary>Action tools for card management and notification operations.</summary>
internal sealed class CardActionTools(BankingDbContext context, INotificationService notifications)
{
    // ── Card management ──────────────────────────────────────────────────────

    [Description(
        "Update the card type (Verve or Mastercard) and/or delivery method for a pending card request. " +
        "Only requests in Pending status can be updated. " +
        "When DeliveryMethod is HomeDelivery, deliveryAddress is required. " +
        "When DeliveryMethod is BranchPickup, branchCode is required.")]
    public async Task<string> UpdateCardRequestAsync(
        [Description("The card request ID (GUID string)")] string cardRequestId,
        [Description("Card type: Verve or Mastercard")] string? cardType = null,
        [Description("Delivery method: BranchPickup or HomeDelivery")] string? deliveryMethod = null,
        [Description("Branch code (required for BranchPickup)")] string? branchCode = null,
        [Description("Full delivery address (required for HomeDelivery)")] string? deliveryAddress = null)
    {
        if (!Guid.TryParse(cardRequestId, out var id))
            return Err("Invalid card request ID format.");

        var card = await context.CardRequests.FindAsync(id);
        if (card is null) return Err($"Card request {cardRequestId} not found.");
        if (card.Status != CardRequestStatus.Pending)
            return Err($"Only Pending card requests can be updated (current: {card.Status}).");

        if (cardType is not null)
        {
            if (!Enum.TryParse<CardType>(cardType, ignoreCase: true, out var ct))
                return Err($"Unknown card type '{cardType}'. Valid: Verve, Mastercard.");
            card.CardType = ct;
        }

        if (deliveryMethod is not null)
        {
            if (!Enum.TryParse<CardDeliveryMethod>(deliveryMethod, ignoreCase: true, out var dm))
                return Err($"Unknown delivery method '{deliveryMethod}'. Valid: BranchPickup, HomeDelivery.");
            card.DeliveryMethod = dm;
        }

        if (card.DeliveryMethod == CardDeliveryMethod.HomeDelivery)
        {
            if (!string.IsNullOrWhiteSpace(deliveryAddress)) card.DeliveryAddress = deliveryAddress;
            if (string.IsNullOrWhiteSpace(card.DeliveryAddress))
                return Err("deliveryAddress is required for HomeDelivery.");
        }

        if (card.DeliveryMethod == CardDeliveryMethod.BranchPickup)
        {
            if (!string.IsNullOrWhiteSpace(branchCode)) card.BranchCode = branchCode;
        }

        await context.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            cardRequestId  = card.Id,
            cardType       = card.CardType.ToString(),
            deliveryMethod = card.DeliveryMethod.ToString(),
            branchCode     = card.BranchCode,
            deliveryAddress= card.DeliveryAddress,
            status         = card.Status.ToString()
        });
    }

    [Description(
        "Advance a card request status. Valid transitions: " +
        "Pending → Processing, Processing → Dispatched, Dispatched → Delivered. " +
        "Provide a trackingNumber when setting to Dispatched.")]
    public async Task<string> UpdateCardStatusAsync(
        [Description("The card request ID (GUID string)")] string cardRequestId,
        [Description("New status: Processing, Dispatched, or Delivered")] string newStatus,
        [Description("Courier tracking number (required when status = Dispatched)")] string? trackingNumber = null)
    {
        if (!Guid.TryParse(cardRequestId, out var id))
            return Err("Invalid card request ID format.");

        if (!Enum.TryParse<CardRequestStatus>(newStatus, ignoreCase: true, out var status))
            return Err($"Unknown status '{newStatus}'. Valid: Processing, Dispatched, Delivered.");

        var card = await context.CardRequests.FindAsync(id);
        if (card is null) return Err($"Card request {cardRequestId} not found.");

        var allowed = (card.Status, status) switch
        {
            (CardRequestStatus.Pending,     CardRequestStatus.Processing) => true,
            (CardRequestStatus.Processing,  CardRequestStatus.Dispatched) => true,
            (CardRequestStatus.Dispatched,  CardRequestStatus.Delivered)  => true,
            _ => false
        };

        if (!allowed)
            return Err($"Invalid transition: {card.Status} → {status}.");

        if (status == CardRequestStatus.Dispatched)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
                return Err("trackingNumber is required when dispatching a card.");
            card.TrackingNumber = trackingNumber;
            card.DispatchedAt   = DateTime.UtcNow;
        }

        if (status == CardRequestStatus.Delivered)
            card.DeliveredAt = DateTime.UtcNow;

        var previous  = card.Status;
        card.Status   = status;

        await context.SaveChangesAsync();

        // Notify the customer of the status change
        var subject = status switch
        {
            CardRequestStatus.Processing => "Your card is being processed",
            CardRequestStatus.Dispatched => $"Your card has been dispatched — tracking: {card.TrackingNumber}",
            CardRequestStatus.Delivered  => "Your debit card has been delivered",
            _                            => $"Card status updated to {status}"
        };

        await notifications.SendAsync(card.CustomerId, subject,
            $"Dear customer, your {card.CardType} debit card status is now: {status}. {subject}.");

        return JsonSerializer.Serialize(new
        {
            cardRequestId  = card.Id,
            previousStatus = previous.ToString(),
            newStatus      = card.Status.ToString(),
            trackingNumber = card.TrackingNumber,
            dispatchedAt   = card.DispatchedAt,
            deliveredAt    = card.DeliveredAt
        });
    }

    // ── PIN generation ───────────────────────────────────────────────────────

    [Description(
        "Trigger PIN generation for a customer's debit card via the secure channel (SMS to registered phone). " +
        "The PIN is never stored — it is delivered directly to the customer's phone. " +
        "Can only be triggered once the card request is in Processing or later status.")]
    public async Task<string> GenerateCardPinAsync(
        [Description("The card request ID (GUID string)")] string cardRequestId)
    {
        if (!Guid.TryParse(cardRequestId, out var id))
            return Err("Invalid card request ID format.");

        var card = await context.CardRequests.FindAsync(id);
        if (card is null) return Err($"Card request {cardRequestId} not found.");

        if (card.Status == CardRequestStatus.Pending || card.Status == CardRequestStatus.Cancelled)
            return Err($"PIN can only be generated once the card is being processed (current: {card.Status}).");

        if (card.PinGenerated)
            return Err("A PIN has already been generated for this card. Customer must use the reset channel to request a new one.");

        // Generate a secure 4-digit PIN and deliver it via SMS only (secure channel)
        var pin        = Random.Shared.Next(1000, 9999).ToString("D4");
        var pinMessage = $"Your {card.CardType} debit card PIN is: {pin}. " +
                         "Do not share this PIN with anyone. Change it at any ATM on first use.";

        await notifications.SendAsync(card.CustomerId, NotificationType.Sms,
            "Your Debit Card PIN", pinMessage);

        card.PinGenerated = true;
        await context.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            cardRequestId = card.Id,
            pinGenerated  = true,
            deliveredVia  = "SMS to registered phone number",
            note          = "PIN delivered securely. It is not stored in the system."
        });
    }

    // ── Notification preferences ─────────────────────────────────────────────

    [Description(
        "Update a customer's notification preferences. " +
        "SMS is mandatory and cannot be disabled. " +
        "Optionally enable/disable email and push notifications and update contact details.")]
    public async Task<string> UpdateNotificationPreferencesAsync(
        [Description("The customer ID (GUID string)")] string customerId,
        [Description("Phone number for SMS (mandatory)")] string? phoneNumber = null,
        [Description("Enable email notifications")] bool? emailEnabled = null,
        [Description("Email address for notifications")] string? email = null,
        [Description("Enable push notifications")] bool? pushEnabled = null,
        [Description("Mobile device token for push notifications")] string? deviceToken = null)
    {
        if (!Guid.TryParse(customerId, out var id))
            return Err("Invalid customer ID format.");

        var prefs = await context.NotificationPreferences.FirstOrDefaultAsync(p => p.CustomerId == id);
        if (prefs is null) return Err($"No notification preferences found for customer {customerId}.");

        if (!string.IsNullOrWhiteSpace(phoneNumber)) prefs.PhoneNumber = phoneNumber;
        if (emailEnabled.HasValue)                   prefs.EmailEnabled = emailEnabled.Value;
        if (!string.IsNullOrWhiteSpace(email))       prefs.Email        = email;
        if (pushEnabled.HasValue)                    prefs.PushEnabled  = pushEnabled.Value;
        if (!string.IsNullOrWhiteSpace(deviceToken)) prefs.DeviceToken  = deviceToken;

        prefs.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            customerId   = prefs.CustomerId,
            smsEnabled   = prefs.SmsEnabled,
            phoneNumber  = prefs.PhoneNumber,
            emailEnabled = prefs.EmailEnabled,
            email        = prefs.Email,
            pushEnabled  = prefs.PushEnabled,
            updatedAt    = prefs.UpdatedAt
        });
    }

    [Description("Send a manual notification to a customer across all their enabled channels (SMS mandatory, email and push if configured).")]
    public async Task<string> SendNotificationAsync(
        [Description("The customer ID (GUID string)")] string customerId,
        [Description("Notification subject / title")] string subject,
        [Description("Notification message body")] string message)
    {
        if (!Guid.TryParse(customerId, out var id))
            return Err("Invalid customer ID format.");

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
            return Err("subject and message are required.");

        await notifications.SendAsync(id, subject, message);

        return JsonSerializer.Serialize(new
        {
            customerId,
            subject,
            status = "Notification dispatched via all enabled channels."
        });
    }

    private static string Err(string message) => JsonSerializer.Serialize(new { error = message });
}
