using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class DigitalEnrollmentConfiguration : IEntityTypeConfiguration<DigitalEnrollment>
{
    public void Configure(EntityTypeBuilder<DigitalEnrollment> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.ServiceType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.Username).HasMaxLength(50).IsRequired();
        builder.Property(d => d.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(d => d.SuspendReason).HasMaxLength(500);

        // One enrollment per customer per service type
        builder.HasIndex(d => new { d.CustomerId, d.ServiceType }).IsUnique();
        builder.HasIndex(d => d.Username).IsUnique();

        builder.ToTable("digital_enrollments");
    }
}
