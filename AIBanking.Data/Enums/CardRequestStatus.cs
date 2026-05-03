namespace AIBanking.Enums;

public enum CardRequestStatus
{
    Pending,      // Request created, awaiting processing
    Processing,   // Card being produced
    Dispatched,   // Card sent to branch or courier
    Delivered,    // Confirmed received by customer
    Cancelled
}
