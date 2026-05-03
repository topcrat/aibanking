using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIBanking.Migrations
{
    /// <inheritdoc />
    public partial class AddNinKycConsentSanctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BvnNumber",
                table: "customers",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KycTier",
                table: "customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NationalIdNumber",
                table: "customers",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyTransactionLimit",
                table: "bank_accounts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "KycTier",
                table: "bank_accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumBalance",
                table: "bank_accounts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SingleTransactionLimit",
                table: "bank_accounts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "ConsentGiven",
                table: "account_applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NinNumber",
                table: "account_applications",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "nin_verifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    NinNumber = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VerifiedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VerifiedDob = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NameMatch = table.Column<bool>(type: "boolean", nullable: false),
                    DobMatch = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nin_verifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_nin_verifications_ApplicationId",
                table: "nin_verifications",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nin_verifications_NinNumber",
                table: "nin_verifications",
                column: "NinNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nin_verifications");

            migrationBuilder.DropColumn(
                name: "BvnNumber",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "KycTier",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "NationalIdNumber",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "DailyTransactionLimit",
                table: "bank_accounts");

            migrationBuilder.DropColumn(
                name: "KycTier",
                table: "bank_accounts");

            migrationBuilder.DropColumn(
                name: "MaximumBalance",
                table: "bank_accounts");

            migrationBuilder.DropColumn(
                name: "SingleTransactionLimit",
                table: "bank_accounts");

            migrationBuilder.DropColumn(
                name: "ConsentGiven",
                table: "account_applications");

            migrationBuilder.DropColumn(
                name: "NinNumber",
                table: "account_applications");
        }
    }
}
