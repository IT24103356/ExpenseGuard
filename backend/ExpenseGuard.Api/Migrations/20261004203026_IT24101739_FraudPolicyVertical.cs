using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExpenseGuard.Api.Migrations
{
    /// <inheritdoc />
    public partial class IT24101739_FraudPolicyVertical : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FraudFlags_ExpenseClaims_ExpenseClaimId",
                table: "FraudFlags");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxAmount",
                table: "Policies",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Policies",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Policies",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "Policies",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "Policies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MinAmount",
                table: "Policies",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PolicyCode",
                table: "Policies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceJson",
                table: "FraudFlags",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FraudEvaluationId",
                table: "FraudFlags",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNote",
                table: "FraudFlags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "FraudFlags",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedBy",
                table: "FraudFlags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RuleCode",
                table: "FraudFlags",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "FraudFlags",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "FraudFlags",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "FraudFlags",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            // Preserve legacy rows while giving every version key and JSON field valid values.
            migrationBuilder.Sql("""
                UPDATE "Policies"
                SET "PolicyCode" = 'LEGACY-' || "PolicyId",
                    "Version" = 1,
                    "Currency" = 'USD',
                    "EffectiveFrom" = TIMESTAMPTZ '1970-01-01 00:00:00+00',
                    "CreatedAt" = NOW(),
                    "IsActive" = TRUE;

                UPDATE "FraudFlags"
                SET "EvidenceJson" = '{}'::jsonb,
                    "RuleCode" = 'LEGACY_FLAG',
                    "Severity" = 'medium',
                    "Source" = 'legacy',
                    "Status" = 'open';
                """);

            migrationBuilder.CreateTable(
                name: "FraudEvaluations",
                columns: table => new
                {
                    FraudEvaluationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExpenseClaimId = table.Column<int>(type: "integer", nullable: false),
                    InputFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NormalizedInvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClaimAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReceiptAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    RiskScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FraudEvaluations", x => x.FraudEvaluationId);
                    table.ForeignKey(
                        name: "FK_FraudEvaluations_ExpenseClaims_ExpenseClaimId",
                        column: x => x.ExpenseClaimId,
                        principalTable: "ExpenseClaims",
                        principalColumn: "ExpenseClaimId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PolicyDesignations",
                columns: table => new
                {
                    PolicyDesignationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PolicyId = table.Column<int>(type: "integer", nullable: false),
                    Designation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyDesignations", x => x.PolicyDesignationId);
                    table.ForeignKey(
                        name: "FK_PolicyDesignations_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "PolicyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PolicyEvaluations",
                columns: table => new
                {
                    PolicyEvaluationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExpenseClaimId = table.Column<int>(type: "integer", nullable: false),
                    PolicyId = table.Column<int>(type: "integer", nullable: true),
                    InputFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyEvaluations", x => x.PolicyEvaluationId);
                    table.ForeignKey(
                        name: "FK_PolicyEvaluations_ExpenseClaims_ExpenseClaimId",
                        column: x => x.ExpenseClaimId,
                        principalTable: "ExpenseClaims",
                        principalColumn: "ExpenseClaimId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PolicyEvaluations_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "PolicyId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PolicyViolations",
                columns: table => new
                {
                    PolicyViolationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PolicyEvaluationId = table.Column<int>(type: "integer", nullable: false),
                    RuleCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyViolations", x => x.PolicyViolationId);
                    table.ForeignKey(
                        name: "FK_PolicyViolations_PolicyEvaluations_PolicyEvaluationId",
                        column: x => x.PolicyEvaluationId,
                        principalTable: "PolicyEvaluations",
                        principalColumn: "PolicyEvaluationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Policies_IsActive_Category_Currency_DepartmentId_EffectiveF~",
                table: "Policies",
                columns: new[] { "IsActive", "Category", "Currency", "DepartmentId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_Policies_PolicyCode_Version",
                table: "Policies",
                columns: new[] { "PolicyCode", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FraudFlags_FraudEvaluationId",
                table: "FraudFlags",
                column: "FraudEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_FraudFlags_Status_Severity_CreatedAt",
                table: "FraudFlags",
                columns: new[] { "Status", "Severity", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FraudEvaluations_ExpenseClaimId_InputFingerprint",
                table: "FraudEvaluations",
                columns: new[] { "ExpenseClaimId", "InputFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FraudEvaluations_NormalizedInvoiceNumber",
                table: "FraudEvaluations",
                column: "NormalizedInvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyDesignations_PolicyId_Designation",
                table: "PolicyDesignations",
                columns: new[] { "PolicyId", "Designation" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PolicyEvaluations_ExpenseClaimId_InputFingerprint",
                table: "PolicyEvaluations",
                columns: new[] { "ExpenseClaimId", "InputFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PolicyEvaluations_PolicyId",
                table: "PolicyEvaluations",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyViolations_PolicyEvaluationId",
                table: "PolicyViolations",
                column: "PolicyEvaluationId");

            migrationBuilder.AddForeignKey(
                name: "FK_FraudFlags_ExpenseClaims_ExpenseClaimId",
                table: "FraudFlags",
                column: "ExpenseClaimId",
                principalTable: "ExpenseClaims",
                principalColumn: "ExpenseClaimId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FraudFlags_FraudEvaluations_FraudEvaluationId",
                table: "FraudFlags",
                column: "FraudEvaluationId",
                principalTable: "FraudEvaluations",
                principalColumn: "FraudEvaluationId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FraudFlags_ExpenseClaims_ExpenseClaimId",
                table: "FraudFlags");

            migrationBuilder.DropForeignKey(
                name: "FK_FraudFlags_FraudEvaluations_FraudEvaluationId",
                table: "FraudFlags");

            migrationBuilder.DropTable(
                name: "FraudEvaluations");

            migrationBuilder.DropTable(
                name: "PolicyDesignations");

            migrationBuilder.DropTable(
                name: "PolicyViolations");

            migrationBuilder.DropTable(
                name: "PolicyEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_Policies_IsActive_Category_Currency_DepartmentId_EffectiveF~",
                table: "Policies");

            migrationBuilder.DropIndex(
                name: "IX_Policies_PolicyCode_Version",
                table: "Policies");

            migrationBuilder.DropIndex(
                name: "IX_FraudFlags_FraudEvaluationId",
                table: "FraudFlags");

            migrationBuilder.DropIndex(
                name: "IX_FraudFlags_Status_Severity_CreatedAt",
                table: "FraudFlags");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "MinAmount",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "PolicyCode",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "EvidenceJson",
                table: "FraudFlags");

            migrationBuilder.DropColumn(
                name: "FraudEvaluationId",
                table: "FraudFlags");

            migrationBuilder.DropColumn(
                name: "ResolutionNote",
                table: "FraudFlags");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "FraudFlags");

            migrationBuilder.DropColumn(
                name: "ResolvedBy",
                table: "FraudFlags");

            migrationBuilder.DropColumn(
                name: "RuleCode",
                table: "FraudFlags");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "FraudFlags");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "FraudFlags");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "FraudFlags");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxAmount",
                table: "Policies",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FraudFlags_ExpenseClaims_ExpenseClaimId",
                table: "FraudFlags",
                column: "ExpenseClaimId",
                principalTable: "ExpenseClaims",
                principalColumn: "ExpenseClaimId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
