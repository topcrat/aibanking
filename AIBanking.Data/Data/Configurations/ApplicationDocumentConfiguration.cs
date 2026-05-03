using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class ApplicationDocumentConfiguration : IEntityTypeConfiguration<ApplicationDocument>
{
    public void Configure(EntityTypeBuilder<ApplicationDocument> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Type)
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(d => d.FileName).HasMaxLength(260).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Content).IsRequired();          // bytea in PostgreSQL
        builder.Property(d => d.UploadedAt).IsRequired();

        builder.ToTable("application_documents");
    }
}
