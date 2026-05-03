using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FullName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.DateOfBirth).HasMaxLength(20);
        builder.Property(c => c.Gender).HasMaxLength(20);
        builder.Property(c => c.PhoneNumber).HasMaxLength(50);
        builder.Property(c => c.ResidenceAddress).HasMaxLength(500);
        builder.Property(c => c.NationalIdNumber).HasMaxLength(11);
        builder.Property(c => c.BvnNumber).HasMaxLength(11);
        builder.Property(c => c.KycTier).HasConversion<int>().IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        // One customer per application
        builder.HasIndex(c => c.ApplicationId).IsUnique();

        builder.ToTable("customers");
    }
}
