using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class WorkflowApprovalConfiguration : IEntityTypeConfiguration<WorkflowApproval>
{
    public void Configure(EntityTypeBuilder<WorkflowApproval> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.StageName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(20).IsRequired();
        builder.Property(a => a.ActedBy).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Comments).HasMaxLength(2000);
        builder.Property(a => a.ActedAt).IsRequired();

        builder.HasIndex(a => a.WorkflowItemId);

        builder.ToTable("workflow_approvals");
    }
}
