using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class BvnVerificationConfiguration : IEntityTypeConfiguration<BvnVerification>
{
    public void Configure(EntityTypeBuilder<BvnVerification> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BvnNumber).HasMaxLength(11).IsRequired();
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(b => b.VerifiedName).HasMaxLength(200);
        builder.Property(b => b.VerifiedDob).HasMaxLength(20);
        builder.Property(b => b.FailureReason).HasMaxLength(500);

        // One-to-one with AccountApplication (latest attempt)
        builder.HasIndex(b => b.ApplicationId).IsUnique();
        builder.HasIndex(b => b.BvnNumber);

        builder.ToTable("bvn_verifications");
    }
}
