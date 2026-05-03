using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class WorkflowDocumentConfiguration : IEntityTypeConfiguration<WorkflowDocument>
{
    public void Configure(EntityTypeBuilder<WorkflowDocument> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileName).HasMaxLength(260).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Content).IsRequired();          // bytea in PostgreSQL
        builder.Property(d => d.UploadedBy).HasMaxLength(200).IsRequired();
        builder.Property(d => d.UploadedAt).IsRequired();

        // FK to WorkflowItem (no navigation property needed on either side)
        builder.HasOne<WorkflowItem>()
               .WithMany()
               .HasForeignKey(d => d.WorkflowId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("workflow_documents");
    }
}
