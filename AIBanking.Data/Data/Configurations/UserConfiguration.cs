using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
               .IsRequired()
               .HasMaxLength(64);

        builder.HasIndex(u => u.Username)
               .IsUnique();

        builder.Property(u => u.PasswordHash)
               .IsRequired()
               .HasMaxLength(256);

        builder.Property(u => u.FullName)
               .IsRequired()
               .HasMaxLength(128);

        builder.Property(u => u.Role)
               .IsRequired()
               .HasMaxLength(32);
    }
}
