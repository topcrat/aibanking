using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AIBanking.Migrations
{
    /// <inheritdoc />
    public partial class GenericWorkflowPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stage",
                table: "workflow_items");

            // 1. Create definition tables and seed data FIRST so the FK can resolve
            migrationBuilder.CreateTable(
                name: "workflow_approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    StageOrder = table.Column<int>(type: "integer", nullable: false),
                    StageName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ActedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_approvals_workflow_items_WorkflowItemId",
                        column: x => x.WorkflowItemId,
                        principalTable: "workflow_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_stage_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StageOrder = table.Column<int>(type: "integer", nullable: false),
                    StageName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequiredRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_stage_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_stage_definitions_workflow_definitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "workflow_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "workflow_definitions",
                columns: new[] { "Id", "Description", "IsActive", "Name" },
                values: new object[] { new Guid("11111111-0000-0000-0000-000000000001"), "Teller → CPC → Team Lead CPC → Compliance", true, "Account Opening Approval" });

            migrationBuilder.InsertData(
                table: "workflow_stage_definitions",
                columns: new[] { "Id", "DefinitionId", "RequiredRole", "StageName", "StageOrder" },
                values: new object[,]
                {
                    { new Guid("22222222-0000-0000-0000-000000000001"), new Guid("11111111-0000-0000-0000-000000000001"), "CPC", "CPC Review", 1 },
                    { new Guid("22222222-0000-0000-0000-000000000002"), new Guid("11111111-0000-0000-0000-000000000001"), "TeamLeadCPC", "Team Lead Review", 2 },
                    { new Guid("22222222-0000-0000-0000-000000000003"), new Guid("11111111-0000-0000-0000-000000000001"), "Compliance", "Compliance", 3 }
                });

            // 2. Now add columns — default to the seeded definition so existing rows pass the FK
            migrationBuilder.AddColumn<int>(
                name: "CurrentStageOrder",
                table: "workflow_items",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "DefinitionId",
                table: "workflow_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("11111111-0000-0000-0000-000000000001"));

            // 3. Point any existing rows at the default definition
            migrationBuilder.Sql(
                "UPDATE workflow_items SET \"DefinitionId\" = '11111111-0000-0000-0000-000000000001', \"CurrentStageOrder\" = 1 WHERE TRUE;");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_items_DefinitionId",
                table: "workflow_items",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_approvals_WorkflowItemId",
                table: "workflow_approvals",
                column: "WorkflowItemId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_stage_definitions_DefinitionId_StageOrder",
                table: "workflow_stage_definitions",
                columns: new[] { "DefinitionId", "StageOrder" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_items_workflow_definitions_DefinitionId",
                table: "workflow_items",
                column: "DefinitionId",
                principalTable: "workflow_definitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workflow_items_workflow_definitions_DefinitionId",
                table: "workflow_items");

            migrationBuilder.DropTable(
                name: "workflow_approvals");

            migrationBuilder.DropTable(
                name: "workflow_stage_definitions");

            migrationBuilder.DropTable(
                name: "workflow_definitions");

            migrationBuilder.DropIndex(
                name: "IX_workflow_items_DefinitionId",
                table: "workflow_items");

            migrationBuilder.DropColumn(
                name: "CurrentStageOrder",
                table: "workflow_items");

            migrationBuilder.DropColumn(
                name: "DefinitionId",
                table: "workflow_items");

            migrationBuilder.AddColumn<string>(
                name: "Stage",
                table: "workflow_items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "CPC");
        }
    }
}
