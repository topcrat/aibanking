using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class FraudAssessmentConfiguration : IEntityTypeConfiguration<FraudAssessment>
{
    public void Configure(EntityTypeBuilder<FraudAssessment> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.RiskLevel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(f => f.Flags).HasMaxLength(2000).IsRequired();
        builder.Property(f => f.ReviewedBy).HasMaxLength(100);
        builder.Property(f => f.ReviewNotes).HasMaxLength(1000);
        builder.Property(f => f.Outcome).HasMaxLength(50);

        builder.HasIndex(f => f.ApplicationId);

        builder.ToTable("fraud_assessments");
    }
}
