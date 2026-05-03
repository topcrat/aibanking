using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class FormSubmissionConfiguration : IEntityTypeConfiguration<FormSubmission>
{
    public void Configure(EntityTypeBuilder<FormSubmission> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SubmittedBy).HasMaxLength(200).IsRequired();
        builder.Property(s => s.SubmittedAt).IsRequired();
        builder.Property(s => s.ValuesJson).HasColumnType("text").IsRequired();

        builder.HasOne(s => s.FormDefinition)
               .WithMany()
               .HasForeignKey(s => s.FormDefinitionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.WorkflowItem)
               .WithOne(w => w.FormSubmission)
               .HasForeignKey<FormSubmission>(s => s.WorkflowItemId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.WorkflowItemId).IsUnique();

        builder.ToTable("form_submissions");
    }
}
