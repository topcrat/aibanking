using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class NinVerificationConfiguration : IEntityTypeConfiguration<NinVerification>
{
    public void Configure(EntityTypeBuilder<NinVerification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.NinNumber).HasMaxLength(11).IsRequired();
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(n => n.VerifiedName).HasMaxLength(200);
        builder.Property(n => n.VerifiedDob).HasMaxLength(20);
        builder.Property(n => n.FailureReason).HasMaxLength(500);

        builder.HasIndex(n => n.ApplicationId).IsUnique();
        builder.HasIndex(n => n.NinNumber);

        builder.ToTable("nin_verifications");
    }
}
