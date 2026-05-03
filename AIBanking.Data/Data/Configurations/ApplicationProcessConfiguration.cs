using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class ApplicationProcessConfiguration : IEntityTypeConfiguration<ApplicationProcess>
{
    public void Configure(EntityTypeBuilder<ApplicationProcess> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(p => p.Status)
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(p => p.Error).HasMaxLength(1000);

        // Unique constraint: one record per process type per application
        builder.HasIndex(p => new { p.ApplicationId, p.Name }).IsUnique();

        builder.ToTable("application_processes");
    }
}
