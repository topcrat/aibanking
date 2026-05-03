using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIBanking.Migrations
{
    /// <inheritdoc />
    public partial class AddReworkToApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReworkNotes",
                table: "account_applications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReworkNotes",
                table: "account_applications");
        }
    }
}
