using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AIBanking.Migrations
{
    /// <inheritdoc />
    public partial class FormDefinitionsAndSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "form_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_form_definitions_workflow_definitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "workflow_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "form_field_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldOrder = table.Column<int>(type: "integer", nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FieldType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Placeholder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OptionsJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_field_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_form_field_definitions_form_definitions_FormDefinitionId",
                        column: x => x.FormDefinitionId,
                        principalTable: "form_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValuesJson = table.Column<string>(type: "text", nullable: false),
                    WorkflowItemId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_form_submissions_form_definitions_FormDefinitionId",
                        column: x => x.FormDefinitionId,
                        principalTable: "form_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_form_submissions_workflow_items_WorkflowItemId",
                        column: x => x.WorkflowItemId,
                        principalTable: "workflow_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_form_submissions_workflow_items_WorkflowItemId1",
                        column: x => x.WorkflowItemId1,
                        principalTable: "workflow_items",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "form_definitions",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Name", "WorkflowDefinitionId" },
                values: new object[] { new Guid("33333333-0000-0000-0000-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "New customer account opening request", true, "Account Opening Form", new Guid("11111111-0000-0000-0000-000000000001") });

            migrationBuilder.InsertData(
                table: "form_field_definitions",
                columns: new[] { "Id", "FieldKey", "FieldOrder", "FieldType", "FormDefinitionId", "IsRequired", "Label", "OptionsJson", "Placeholder" },
                values: new object[,]
                {
                    { new Guid("44444444-0000-0000-0000-000000000001"), "fullName", 1, "Text", new Guid("33333333-0000-0000-0000-000000000001"), true, "Full Name", null, "e.g. John Doe" },
                    { new Guid("44444444-0000-0000-0000-000000000002"), "dateOfBirth", 2, "Date", new Guid("33333333-0000-0000-0000-000000000001"), true, "Date of Birth", null, null },
                    { new Guid("44444444-0000-0000-0000-000000000003"), "gender", 3, "Select", new Guid("33333333-0000-0000-0000-000000000001"), true, "Gender", "[\"Male\",\"Female\",\"Other\"]", null },
                    { new Guid("44444444-0000-0000-0000-000000000004"), "phoneNumber", 4, "Text", new Guid("33333333-0000-0000-0000-000000000001"), true, "Phone Number", null, "e.g. 08012345678" },
                    { new Guid("44444444-0000-0000-0000-000000000005"), "bvnNumber", 5, "Text", new Guid("33333333-0000-0000-0000-000000000001"), true, "BVN", null, "11-digit BVN" },
                    { new Guid("44444444-0000-0000-0000-000000000006"), "ninNumber", 6, "Text", new Guid("33333333-0000-0000-0000-000000000001"), false, "NIN", null, "11-digit NIN (optional)" },
                    { new Guid("44444444-0000-0000-0000-000000000007"), "residenceAddress", 7, "TextArea", new Guid("33333333-0000-0000-0000-000000000001"), true, "Residence Address", null, "Full residential address" },
                    { new Guid("44444444-0000-0000-0000-000000000008"), "idDocument", 8, "File", new Guid("33333333-0000-0000-0000-000000000001"), true, "ID Document", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_form_definitions_WorkflowDefinitionId",
                table: "form_definitions",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_form_field_definitions_FormDefinitionId_FieldOrder",
                table: "form_field_definitions",
                columns: new[] { "FormDefinitionId", "FieldOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_form_submissions_FormDefinitionId",
                table: "form_submissions",
                column: "FormDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_form_submissions_WorkflowItemId",
                table: "form_submissions",
                column: "WorkflowItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_form_submissions_WorkflowItemId1",
                table: "form_submissions",
                column: "WorkflowItemId1",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "form_field_definitions");

            migrationBuilder.DropTable(
                name: "form_submissions");

            migrationBuilder.DropTable(
                name: "form_definitions");
        }
    }
}
