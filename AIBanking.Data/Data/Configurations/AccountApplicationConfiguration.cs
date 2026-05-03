using AIBanking.Enums;
using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class AccountApplicationConfiguration : IEntityTypeConfiguration<AccountApplication>
{
    public void Configure(EntityTypeBuilder<AccountApplication> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status)
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();

        // Store ExtractedPersonInfo as a JSON column (nullable — null until extraction runs)
        builder.OwnsOne(a => a.ExtractedInfo, info =>
        {
            info.ToJson();
            info.Property(i => i.FullName).HasMaxLength(200);
            info.Property(i => i.DateOfBirth).HasMaxLength(20);
            info.Property(i => i.Gender).HasMaxLength(20);
            info.Property(i => i.PhoneNumber).HasMaxLength(50);
            info.Property(i => i.ResidenceAddress).HasMaxLength(500);
            info.Property(i => i.NationalIdNumber).HasMaxLength(11);
        });

        builder.HasMany(a => a.Documents)
               .WithOne()
               .HasForeignKey(d => d.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Processes)
               .WithOne()
               .HasForeignKey(p => p.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.BvnNumber).HasMaxLength(11);
        builder.Property(a => a.NinNumber).HasMaxLength(11);
        builder.Property(a => a.ConsentGiven).IsRequired().HasDefaultValue(false);
        builder.Property(a => a.ReworkNotes).HasMaxLength(2000);

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();

        builder.ToTable("account_applications");
    }
}
