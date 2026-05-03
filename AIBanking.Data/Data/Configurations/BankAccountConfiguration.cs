using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AccountNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(a => a.AccountNumber).IsUnique();

        builder.Property(a => a.AccountType)
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(a => a.KycTier).HasConversion<int>().IsRequired();
        builder.Property(a => a.SingleTransactionLimit).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(a => a.DailyTransactionLimit).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(a => a.MaximumBalance).HasColumnType("numeric(18,2)").IsRequired();

        builder.Property(a => a.CreatedAt).IsRequired();

        // One account per application
        builder.HasIndex(a => a.ApplicationId).IsUnique();

        builder.HasOne<Customer>()
               .WithMany()
               .HasForeignKey(a => a.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("bank_accounts");
    }
}
