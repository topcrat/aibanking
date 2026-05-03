using AIBanking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIBanking.Data.Configurations;

public class FormDefinitionConfiguration : IEntityTypeConfiguration<FormDefinition>
{
    public static readonly Guid AccountOpeningFormId =
        Guid.Parse("33333333-0000-0000-0000-000000000001");

    public static readonly Guid LoanBookingFormId =
        Guid.Parse("33333333-0000-0000-0000-000000000002");

    public void Configure(EntityTypeBuilder<FormDefinition> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(1000);
        builder.Property(f => f.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(f => f.CreatedAt).IsRequired();

        builder.HasOne(f => f.WorkflowDefinition)
               .WithMany()
               .HasForeignKey(f => f.WorkflowDefinitionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.Fields)
               .WithOne(ff => ff.FormDefinition)
               .HasForeignKey(ff => ff.FormDefinitionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("form_definitions");

        // Seed form definitions
        builder.HasData(
            new FormDefinition
            {
                Id                   = AccountOpeningFormId,
                Name                 = "Account Opening Form",
                Description          = "New customer account opening request",
                WorkflowDefinitionId = WorkflowDefinitionConfiguration.DefaultDefinitionId,
                IsActive             = true,
                CreatedAt            = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            },
            new FormDefinition
            {
                Id                   = LoanBookingFormId,
                Name                 = "Loan Booking Form",
                Description          = "New loan application request",
                WorkflowDefinitionId = WorkflowDefinitionConfiguration.LoanDefinitionId,
                IsActive             = true,
                CreatedAt            = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            });
    }
}

public class FormFieldDefinitionConfiguration : IEntityTypeConfiguration<FormFieldDefinition>
{
    public void Configure(EntityTypeBuilder<FormFieldDefinition> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.FieldKey).HasMaxLength(100).IsRequired();
        builder.Property(f => f.Label).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Placeholder).HasMaxLength(200);
        builder.Property(f => f.OptionsJson).HasColumnType("text");
        builder.Property(f => f.FieldType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(f => f.FieldOrder).IsRequired();

        builder.HasIndex(f => new { f.FormDefinitionId, f.FieldOrder }).IsUnique();

        builder.ToTable("form_field_definitions");

        var aoFormId   = FormDefinitionConfiguration.AccountOpeningFormId;
        var loanFormId = FormDefinitionConfiguration.LoanBookingFormId;

        builder.HasData(
            // Account Opening fields
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000001"), FormDefinitionId = aoFormId, FieldOrder = 1, FieldKey = "fullName",         Label = "Full Name",          FieldType = FormFieldType.Text,     IsRequired = true,  Placeholder = "e.g. John Doe" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000002"), FormDefinitionId = aoFormId, FieldOrder = 2, FieldKey = "dateOfBirth",      Label = "Date of Birth",      FieldType = FormFieldType.Date,     IsRequired = true  },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000003"), FormDefinitionId = aoFormId, FieldOrder = 3, FieldKey = "gender",           Label = "Gender",             FieldType = FormFieldType.Select,   IsRequired = true,  OptionsJson = "[\"Male\",\"Female\",\"Other\"]" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000004"), FormDefinitionId = aoFormId, FieldOrder = 4, FieldKey = "phoneNumber",      Label = "Phone Number",       FieldType = FormFieldType.Text,     IsRequired = true,  Placeholder = "e.g. 08012345678" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000005"), FormDefinitionId = aoFormId, FieldOrder = 5, FieldKey = "bvnNumber",        Label = "BVN",                FieldType = FormFieldType.Text,     IsRequired = true,  Placeholder = "11-digit BVN" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000006"), FormDefinitionId = aoFormId, FieldOrder = 6, FieldKey = "ninNumber",        Label = "NIN",                FieldType = FormFieldType.Text,     IsRequired = false, Placeholder = "11-digit NIN (optional)" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000007"), FormDefinitionId = aoFormId, FieldOrder = 7, FieldKey = "residenceAddress", Label = "Residence Address",  FieldType = FormFieldType.TextArea, IsRequired = true,  Placeholder = "Full residential address" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000008"), FormDefinitionId = aoFormId, FieldOrder = 8, FieldKey = "idDocument",      Label = "ID Document",        FieldType = FormFieldType.File,     IsRequired = true  },

            // Loan Booking fields
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000009"), FormDefinitionId = loanFormId, FieldOrder = 1,  FieldKey = "customerName",     Label = "Customer Full Name",     FieldType = FormFieldType.Text,     IsRequired = true,  Placeholder = "As on ID" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000010"), FormDefinitionId = loanFormId, FieldOrder = 2,  FieldKey = "accountNumber",    Label = "Account Number",         FieldType = FormFieldType.Text,     IsRequired = true,  Placeholder = "Existing BNK account number" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000011"), FormDefinitionId = loanFormId, FieldOrder = 3,  FieldKey = "bvnNumber",        Label = "BVN",                    FieldType = FormFieldType.Text,     IsRequired = true,  Placeholder = "11-digit BVN" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000012"), FormDefinitionId = loanFormId, FieldOrder = 4,  FieldKey = "loanType",         Label = "Loan Type",              FieldType = FormFieldType.Select,   IsRequired = true,  OptionsJson = "[\"Personal\",\"Business\",\"Mortgage\",\"Auto\",\"Education\"]" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000013"), FormDefinitionId = loanFormId, FieldOrder = 5,  FieldKey = "loanAmount",       Label = "Loan Amount (₦)",        FieldType = FormFieldType.Number,   IsRequired = true,  Placeholder = "e.g. 500000" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000014"), FormDefinitionId = loanFormId, FieldOrder = 6,  FieldKey = "loanTenor",        Label = "Loan Tenor (months)",    FieldType = FormFieldType.Number,   IsRequired = true,  Placeholder = "e.g. 12" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000015"), FormDefinitionId = loanFormId, FieldOrder = 7,  FieldKey = "loanPurpose",      Label = "Purpose of Loan",        FieldType = FormFieldType.TextArea, IsRequired = true,  Placeholder = "Describe the intended use of funds" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000016"), FormDefinitionId = loanFormId, FieldOrder = 8,  FieldKey = "monthlyIncome",    Label = "Monthly Income (₦)",     FieldType = FormFieldType.Number,   IsRequired = true,  Placeholder = "e.g. 150000" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000017"), FormDefinitionId = loanFormId, FieldOrder = 9,  FieldKey = "employerName",     Label = "Employer / Business",    FieldType = FormFieldType.Text,     IsRequired = true,  Placeholder = "Employer or business name" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000018"), FormDefinitionId = loanFormId, FieldOrder = 10, FieldKey = "collateralType",   Label = "Collateral Type",        FieldType = FormFieldType.Select,   IsRequired = false, OptionsJson = "[\"None\",\"Property\",\"Vehicle\",\"Equipment\",\"Other\"]" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000019"), FormDefinitionId = loanFormId, FieldOrder = 11, FieldKey = "collateralValue",  Label = "Collateral Value (₦)",   FieldType = FormFieldType.Number,   IsRequired = false, Placeholder = "Estimated market value" },
            new FormFieldDefinition { Id = Guid.Parse("44444444-0000-0000-0000-000000000020"), FormDefinitionId = loanFormId, FieldOrder = 12, FieldKey = "supportingDoc",    Label = "Supporting Document",    FieldType = FormFieldType.File,     IsRequired = true  }
        );
    }
}
