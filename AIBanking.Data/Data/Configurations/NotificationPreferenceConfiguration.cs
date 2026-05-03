using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.HasKey(n => n.Id);

        // One customer → one preference record
        builder.HasIndex(n => n.CustomerId).IsUnique();

        builder.Property(n => n.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(n => n.Email).HasMaxLength(200);
        builder.Property(n => n.DeviceToken).HasMaxLength(500);

        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.UpdatedAt).IsRequired();

        builder.ToTable("notification_preferences");
    }
}
