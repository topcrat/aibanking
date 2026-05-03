using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AIBanking.Migrations
{
    /// <inheritdoc />
    public partial class LoanBookingWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "workflow_definitions",
                columns: new[] { "Id", "Description", "IsActive", "Name" },
                values: new object[] { new Guid("11111111-0000-0000-0000-000000000002"), "Credit Analyst → Team Lead Credit → Compliance", true, "Loan Booking Approval" });

            migrationBuilder.InsertData(
                table: "form_definitions",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Name", "WorkflowDefinitionId" },
                values: new object[] { new Guid("33333333-0000-0000-0000-000000000002"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "New loan application request", true, "Loan Booking Form", new Guid("11111111-0000-0000-0000-000000000002") });

            migrationBuilder.InsertData(
                table: "workflow_stage_definitions",
                columns: new[] { "Id", "DefinitionId", "RequiredRole", "StageName", "StageOrder" },
                values: new object[,]
                {
                    { new Guid("22222222-0000-0000-0000-000000000004"), new Guid("11111111-0000-0000-0000-000000000002"), "CreditAnalyst", "Credit Analyst Review", 1 },
                    { new Guid("22222222-0000-0000-0000-000000000005"), new Guid("11111111-0000-0000-0000-000000000002"), "TeamLeadCredit", "Team Lead Credit", 2 },
                    { new Guid("22222222-0000-0000-0000-000000000006"), new Guid("11111111-0000-0000-0000-000000000002"), "Compliance", "Compliance", 3 }
                });

            migrationBuilder.InsertData(
                table: "form_field_definitions",
                columns: new[] { "Id", "FieldKey", "FieldOrder", "FieldType", "FormDefinitionId", "IsRequired", "Label", "OptionsJson", "Placeholder" },
                values: new object[,]
                {
                    { new Guid("44444444-0000-0000-0000-000000000009"), "customerName", 1, "Text", new Guid("33333333-0000-0000-0000-000000000002"), true, "Customer Full Name", null, "As on ID" },
                    { new Guid("44444444-0000-0000-0000-000000000010"), "accountNumber", 2, "Text", new Guid("33333333-0000-0000-0000-000000000002"), true, "Account Number", null, "Existing BNK account number" },
                    { new Guid("44444444-0000-0000-0000-000000000011"), "bvnNumber", 3, "Text", new Guid("33333333-0000-0000-0000-000000000002"), true, "BVN", null, "11-digit BVN" },
                    { new Guid("44444444-0000-0000-0000-000000000012"), "loanType", 4, "Select", new Guid("33333333-0000-0000-0000-000000000002"), true, "Loan Type", "[\"Personal\",\"Business\",\"Mortgage\",\"Auto\",\"Education\"]", null },
                    { new Guid("44444444-0000-0000-0000-000000000013"), "loanAmount", 5, "Number", new Guid("33333333-0000-0000-0000-000000000002"), true, "Loan Amount (₦)", null, "e.g. 500000" },
                    { new Guid("44444444-0000-0000-0000-000000000014"), "loanTenor", 6, "Number", new Guid("33333333-0000-0000-0000-000000000002"), true, "Loan Tenor (months)", null, "e.g. 12" },
                    { new Guid("44444444-0000-0000-0000-000000000015"), "loanPurpose", 7, "TextArea", new Guid("33333333-0000-0000-0000-000000000002"), true, "Purpose of Loan", null, "Describe the intended use of funds" },
                    { new Guid("44444444-0000-0000-0000-000000000016"), "monthlyIncome", 8, "Number", new Guid("33333333-0000-0000-0000-000000000002"), true, "Monthly Income (₦)", null, "e.g. 150000" },
                    { new Guid("44444444-0000-0000-0000-000000000017"), "employerName", 9, "Text", new Guid("33333333-0000-0000-0000-000000000002"), true, "Employer / Business", null, "Employer or business name" },
                    { new Guid("44444444-0000-0000-0000-000000000018"), "collateralType", 10, "Select", new Guid("33333333-0000-0000-0000-000000000002"), false, "Collateral Type", "[\"None\",\"Property\",\"Vehicle\",\"Equipment\",\"Other\"]", null },
                    { new Guid("44444444-0000-0000-0000-000000000019"), "collateralValue", 11, "Number", new Guid("33333333-0000-0000-0000-000000000002"), false, "Collateral Value (₦)", null, "Estimated market value" },
                    { new Guid("44444444-0000-0000-0000-000000000020"), "supportingDoc", 12, "File", new Guid("33333333-0000-0000-0000-000000000002"), true, "Supporting Document", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "form_field_definitions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "form_field_definitions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "form_field_definitions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "form_field_definitions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "form_field_definitions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "form_field_definitions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                table: "form_field_definitions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                table: "form_field_definitions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                table: "form_field_definitions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                table: "form_field_definitions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000018"));

            migrationBuilder.DeleteData(
                table: "form_field_definitions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000019"));

            migrationBuilder.DeleteData(
                table: "form_field_definitions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-0000-0000-0000-000000000020"));

            migrationBuilder.DeleteData(
                table: "workflow_stage_definitions",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "workflow_stage_definitions",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "workflow_stage_definitions",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "form_definitions",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "workflow_definitions",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000002"));
        }
    }
}
