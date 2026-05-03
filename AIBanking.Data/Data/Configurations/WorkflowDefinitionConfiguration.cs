using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    // Fixed GUIDs so the seed is stable across migrations
    public static readonly Guid DefaultDefinitionId =
        Guid.Parse("11111111-0000-0000-0000-000000000001");

    public static readonly Guid LoanDefinitionId =
        Guid.Parse("11111111-0000-0000-0000-000000000002");

    public static readonly (Guid Id, int Order, string Name, string Role)[] DefaultStages =
    [
        (Guid.Parse("22222222-0000-0000-0000-000000000001"), 1, "CPC Review",      "CPC"),
        (Guid.Parse("22222222-0000-0000-0000-000000000002"), 2, "Team Lead Review", "TeamLeadCPC"),
        (Guid.Parse("22222222-0000-0000-0000-000000000003"), 3, "Compliance",       "Compliance"),
    ];

    public static readonly (Guid Id, int Order, string Name, string Role)[] LoanStages =
    [
        (Guid.Parse("22222222-0000-0000-0000-000000000004"), 1, "Credit Analyst Review", "CreditAnalyst"),
        (Guid.Parse("22222222-0000-0000-0000-000000000005"), 2, "Team Lead Credit",       "TeamLeadCredit"),
        (Guid.Parse("22222222-0000-0000-0000-000000000006"), 3, "Compliance",              "Compliance"),
    ];

    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(1000);
        builder.Property(d => d.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasMany(d => d.Stages)
               .WithOne(s => s.Definition)
               .HasForeignKey(s => s.DefinitionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("workflow_definitions");

        // Seed the default 3-level pipeline
        builder.HasData(
            new WorkflowDefinition
            {
                Id          = DefaultDefinitionId,
                Name        = "Account Opening Approval",
                Description = "Teller → CPC → Team Lead CPC → Compliance",
                IsActive    = true,
            },
            new WorkflowDefinition
            {
                Id          = LoanDefinitionId,
                Name        = "Loan Booking Approval",
                Description = "Credit Analyst → Team Lead Credit → Compliance",
                IsActive    = true,
            });
    }
}

public class WorkflowStageDefinitionConfiguration : IEntityTypeConfiguration<WorkflowStageDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowStageDefinition> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.StageName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.RequiredRole).HasMaxLength(100).IsRequired();
        builder.Property(s => s.StageOrder).IsRequired();

        builder.HasIndex(s => new { s.DefinitionId, s.StageOrder }).IsUnique();

        builder.ToTable("workflow_stage_definitions");

        // Seed stages for both pipelines
        var accountStages = WorkflowDefinitionConfiguration.DefaultStages.Select(s => new WorkflowStageDefinition
        {
            Id           = s.Id,
            DefinitionId = WorkflowDefinitionConfiguration.DefaultDefinitionId,
            StageOrder   = s.Order,
            StageName    = s.Name,
            RequiredRole = s.Role,
        });

        var loanStages = WorkflowDefinitionConfiguration.LoanStages.Select(s => new WorkflowStageDefinition
        {
            Id           = s.Id,
            DefinitionId = WorkflowDefinitionConfiguration.LoanDefinitionId,
            StageOrder   = s.Order,
            StageName    = s.Name,
            RequiredRole = s.Role,
        });

        builder.HasData([.. accountStages, .. loanStages]);
    }
}
