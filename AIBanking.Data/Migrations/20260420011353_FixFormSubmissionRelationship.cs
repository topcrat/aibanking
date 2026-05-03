using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIBanking.Migrations
{
    /// <inheritdoc />
    public partial class FixFormSubmissionRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_form_submissions_workflow_items_WorkflowItemId1",
                table: "form_submissions");

            migrationBuilder.DropIndex(
                name: "IX_form_submissions_WorkflowItemId1",
                table: "form_submissions");

            migrationBuilder.DropColumn(
                name: "WorkflowItemId1",
                table: "form_submissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowItemId1",
                table: "form_submissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_form_submissions_WorkflowItemId1",
                table: "form_submissions",
                column: "WorkflowItemId1",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_form_submissions_workflow_items_WorkflowItemId1",
                table: "form_submissions",
                column: "WorkflowItemId1",
                principalTable: "workflow_items",
                principalColumn: "Id");
        }
    }
}
