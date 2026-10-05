using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExpenseGuard.Api.Migrations
{
    /// <inheritdoc />
    public partial class BudgetDepartment_IT24102706 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Budgets_DepartmentId_Period",
                table: "Budgets");

            migrationBuilder.RenameColumn(
                name: "Period",
                table: "Budgets",
                newName: "Name");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Budgets",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "DepartmentName",
                table: "Departments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Departments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Departments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Departments",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Departments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "Departments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Budgets",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Budgets",
                type: "character(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Budgets",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PeriodEnd",
                table: "Budgets",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(2000, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "PeriodStart",
                table: "Budgets",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(2000, 1, 1));

            migrationBuilder.AddColumn<decimal>(
                name: "ReservedAmount",
                table: "Budgets",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Budgets",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "Budgets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("""
                UPDATE "Departments"
                SET "Code" = 'DEPT-' || "DepartmentId",
                    "CreatedAt" = CURRENT_TIMESTAMP,
                    "UpdatedAt" = CURRENT_TIMESTAMP,
                    "Version" = md5(random()::text || clock_timestamp()::text)::uuid;
                UPDATE "Budgets"
                SET "Currency" = 'LKR',
                    "PeriodStart" = DATE '2000-01-01' + "BudgetId",
                    "PeriodEnd" = DATE '2000-01-01' + "BudgetId",
                    "CreatedAt" = CURRENT_TIMESTAMP,
                    "UpdatedAt" = CURRENT_TIMESTAMP,
                    "Version" = md5(random()::text || clock_timestamp()::text)::uuid;
                """);

            migrationBuilder.CreateTable(
                name: "BudgetAlerts",
                columns: table => new
                {
                    BudgetAlertId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BudgetId = table.Column<int>(type: "integer", nullable: false),
                    ThresholdPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    UtilizationPercent = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetAlerts", x => x.BudgetAlertId);
                    table.ForeignKey(
                        name: "FK_BudgetAlerts_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "BudgetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BudgetTransactions",
                columns: table => new
                {
                    BudgetTransactionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BudgetId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReservedBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SpentBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetTransactions", x => x.BudgetTransactionId);
                    table.CheckConstraint("CK_BudgetTransactions_Amount", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_BudgetTransactions_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "BudgetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_DepartmentId_PeriodStart_PeriodEnd_Currency",
                table: "Budgets",
                columns: new[] { "DepartmentId", "PeriodStart", "PeriodEnd", "Currency" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Budgets_Balances",
                table: "Budgets",
                sql: "\"AllocatedAmount\" >= 0 AND \"ReservedAmount\" >= 0 AND \"SpentAmount\" >= 0 AND \"ReservedAmount\" + \"SpentAmount\" <= \"AllocatedAmount\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Budgets_Dates",
                table: "Budgets",
                sql: "\"PeriodEnd\" >= \"PeriodStart\"");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetAlerts_BudgetId_ThresholdPercent_Status",
                table: "BudgetAlerts",
                columns: new[] { "BudgetId", "ThresholdPercent", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_BudgetId_CreatedAt",
                table: "BudgetTransactions",
                columns: new[] { "BudgetId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_BudgetId_IdempotencyKey",
                table: "BudgetTransactions",
                columns: new[] { "BudgetId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetAlerts");

            migrationBuilder.DropTable(
                name: "BudgetTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Departments_Code",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Budgets_DepartmentId_PeriodStart_PeriodEnd_Currency",
                table: "Budgets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Budgets_Balances",
                table: "Budgets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Budgets_Dates",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "PeriodEnd",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "PeriodStart",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "ReservedAmount",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Budgets");

            migrationBuilder.AlterColumn<string>(
                name: "DepartmentName",
                table: "Departments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Budgets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Budgets",
                newName: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_DepartmentId_Period",
                table: "Budgets",
                columns: new[] { "DepartmentId", "Period" },
                unique: true);
        }
    }
}
