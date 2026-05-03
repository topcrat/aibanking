using AIBanking.Enums;

namespace AIBanking.Models;

public class CardRequest
{
    public Guid               Id             { get; set; }
    public Guid               AccountId      { get; set; }     // FK → BankAccount
    public Guid               CustomerId     { get; set; }     // FK → Customer
    public Guid               ApplicationId  { get; set; }     // FK → AccountApplication

    public CardType           CardType       { get; set; } = CardType.Mastercard;
    public CardDeliveryMethod DeliveryMethod { get; set; } = CardDeliveryMethod.BranchPickup;

    /// <summary>Branch code for pickup; populated when DeliveryMethod = BranchPickup.</summary>
    public string?            BranchCode     { get; set; }

    /// <summary>Full delivery address; populated when DeliveryMethod = HomeDelivery.</summary>
    public string?            DeliveryAddress { get; set; }

    public CardRequestStatus  Status         { get; set; } = CardRequestStatus.Pending;

    /// <summary>Courier or dispatch tracking reference.</summary>
    public string?            TrackingNumber  { get; set; }

    /// <summary>True once the customer has set their PIN via the secure channel.</summary>
    public bool               PinGenerated   { get; set; }

    public DateTime           RequestedAt    { get; set; }
    public DateTime?          DispatchedAt   { get; set; }
    public DateTime?          DeliveredAt    { get; set; }
}
