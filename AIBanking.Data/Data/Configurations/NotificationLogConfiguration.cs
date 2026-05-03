using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(n => n.Status)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(n => n.Recipient).HasMaxLength(500).IsRequired();
        builder.Property(n => n.Subject).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(2000).IsRequired();
        builder.Property(n => n.ErrorMessage).HasMaxLength(1000);
        builder.Property(n => n.SentAt).IsRequired();

        builder.HasIndex(n => n.CustomerId);
        builder.HasIndex(n => n.SentAt);

        builder.ToTable("notification_logs");
    }
}
