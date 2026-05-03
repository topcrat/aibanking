using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class OnboardingMetricConfiguration : IEntityTypeConfiguration<OnboardingMetric>
{
    public void Configure(EntityTypeBuilder<OnboardingMetric> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.LastStage).HasMaxLength(50).IsRequired();
        builder.Property(o => o.Outcome).HasMaxLength(30).IsRequired();
        builder.Property(o => o.FailureReason).HasMaxLength(500);

        builder.HasIndex(o => o.ApplicationId).IsUnique();
        builder.HasIndex(o => o.StartedAt);
        builder.HasIndex(o => o.Outcome);

        builder.ToTable("onboarding_metrics");
    }
}
