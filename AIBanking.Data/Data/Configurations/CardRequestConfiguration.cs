using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class CardRequestConfiguration : IEntityTypeConfiguration<CardRequest>
{
    public void Configure(EntityTypeBuilder<CardRequest> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CardType)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(c => c.DeliveryMethod)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(c => c.Status)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(c => c.BranchCode).HasMaxLength(20);
        builder.Property(c => c.DeliveryAddress).HasMaxLength(500);
        builder.Property(c => c.TrackingNumber).HasMaxLength(100);

        builder.Property(c => c.RequestedAt).IsRequired();

        // One account → one card request per account (unique constraint)
        builder.HasIndex(c => c.AccountId).IsUnique();

        builder.ToTable("card_requests");
    }
}
