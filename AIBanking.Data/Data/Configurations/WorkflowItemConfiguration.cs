using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class WorkflowItemConfiguration : IEntityTypeConfiguration<WorkflowItem>
{
    public void Configure(EntityTypeBuilder<WorkflowItem> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Title).HasMaxLength(300).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(2000);
        builder.Property(w => w.SubmittedBy).HasMaxLength(200).IsRequired();
        builder.Property(w => w.ReviewedBy).HasMaxLength(200);
        builder.Property(w => w.Comments).HasMaxLength(2000);
        builder.Property(w => w.CurrentStageOrder).IsRequired();

        builder.Property(w => w.Status)
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();

        builder.HasOne(w => w.Definition)
               .WithMany()
               .HasForeignKey(w => w.DefinitionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.Approvals)
               .WithOne()
               .HasForeignKey(a => a.WorkflowItemId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(w => w.CreatedAt).IsRequired();
        builder.Property(w => w.UpdatedAt).IsRequired();

        builder.ToTable("workflow_items");
    }
}
