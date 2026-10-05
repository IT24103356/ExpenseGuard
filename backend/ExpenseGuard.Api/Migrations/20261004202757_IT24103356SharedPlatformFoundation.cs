using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExpenseGuard.Api.Migrations
{
    /// <inheritdoc />
    public partial class IT24103356SharedPlatformFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanManageRoles",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanProcessPayments",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PermissionsJson",
                table: "Roles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Reimbursements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Reimbursements",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "Reimbursements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Reimbursements",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentId",
                table: "Reimbursements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentProvider",
                table: "Reimbursements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "Reimbursements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedAt",
                table: "Reimbursements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "Reimbursements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowTemplates",
                columns: table => new
                {
                    ApprovalWorkflowTemplateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflowTemplates", x => x.ApprovalWorkflowTemplateId);
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowTemplates_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId");
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<int>(type: "integer", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    DataJson = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReimbursementId = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: false),
                    ExternalTransactionId = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.PaymentTransactionId);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Reimbursements_ReimbursementId",
                        column: x => x.ReimbursementId,
                        principalTable: "Reimbursements",
                        principalColumn: "ReimbursementId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowExecutions",
                columns: table => new
                {
                    WorkflowExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseClaimId = table.Column<int>(type: "integer", nullable: false),
                    Objective = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    StateJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowExecutions", x => x.WorkflowExecutionId);
                    table.ForeignKey(
                        name: "FK_WorkflowExecutions_ExpenseClaims_ExpenseClaimId",
                        column: x => x.ExpenseClaimId,
                        principalTable: "ExpenseClaims",
                        principalColumn: "ExpenseClaimId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalProcesses",
                columns: table => new
                {
                    ApprovalProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReimbursementId = table.Column<int>(type: "integer", nullable: false),
                    ApprovalWorkflowTemplateId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CurrentSequence = table.Column<int>(type: "integer", nullable: false),
                    TemplateSnapshotJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalProcesses", x => x.ApprovalProcessId);
                    table.ForeignKey(
                        name: "FK_ApprovalProcesses_ApprovalWorkflowTemplates_ApprovalWorkflo~",
                        column: x => x.ApprovalWorkflowTemplateId,
                        principalTable: "ApprovalWorkflowTemplates",
                        principalColumn: "ApprovalWorkflowTemplateId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalProcesses_Reimbursements_ReimbursementId",
                        column: x => x.ReimbursementId,
                        principalTable: "Reimbursements",
                        principalColumn: "ReimbursementId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalStageDefinitions",
                columns: table => new
                {
                    ApprovalStageDefinitionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApprovalWorkflowTemplateId = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    RequiredRole = table.Column<string>(type: "text", nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    MaximumAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalStageDefinitions", x => x.ApprovalStageDefinitionId);
                    table.ForeignKey(
                        name: "FK_ApprovalStageDefinitions_ApprovalWorkflowTemplates_Approval~",
                        column: x => x.ApprovalWorkflowTemplateId,
                        principalTable: "ApprovalWorkflowTemplates",
                        principalColumn: "ApprovalWorkflowTemplateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowSteps",
                columns: table => new
                {
                    WorkflowStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    InputSnapshotJson = table.Column<string>(type: "text", nullable: true),
                    OutputSnapshotJson = table.Column<string>(type: "text", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSteps", x => x.WorkflowStepId);
                    table.ForeignKey(
                        name: "FK_WorkflowSteps_WorkflowExecutions_WorkflowExecutionId",
                        column: x => x.WorkflowExecutionId,
                        principalTable: "WorkflowExecutions",
                        principalColumn: "WorkflowExecutionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalSteps",
                columns: table => new
                {
                    ApprovalStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    RequiredRole = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DecidedByEmployeeId = table.Column<int>(type: "integer", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    StageSnapshotJson = table.Column<string>(type: "text", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalSteps", x => x.ApprovalStepId);
                    table.ForeignKey(
                        name: "FK_ApprovalSteps_ApprovalProcesses_ApprovalProcessId",
                        column: x => x.ApprovalProcessId,
                        principalTable: "ApprovalProcesses",
                        principalColumn: "ApprovalProcessId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalSteps_Employees_DecidedByEmployeeId",
                        column: x => x.DecidedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ToolExecutions",
                columns: table => new
                {
                    ToolExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolName = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RequestJson = table.Column<string>(type: "text", nullable: true),
                    ResponseJson = table.Column<string>(type: "text", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolExecutions", x => x.ToolExecutionId);
                    table.ForeignKey(
                        name: "FK_ToolExecutions_WorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "WorkflowSteps",
                        principalColumn: "WorkflowStepId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValidationResults",
                columns: table => new
                {
                    ValidationResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    Validator = table.Column<string>(type: "text", nullable: false),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    DetailsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationResults", x => x.ValidationResultId);
                    table.ForeignKey(
                        name: "FK_ValidationResults_WorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "WorkflowSteps",
                        principalColumn: "WorkflowStepId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                UPDATE "Reimbursements"
                SET "IdempotencyKey" = 'legacy:' || "ReimbursementId",
                    "Currency" = 'LKR',
                    "RequestedAt" = "CreatedAt"
                WHERE "IdempotencyKey" = '';

                INSERT INTO "Roles" ("RoleName", "ApprovalLimit", "CanApprove", "CanManageRoles", "CanProcessPayments", "PermissionsJson")
                SELECT 'Employee', 0, false, false, false, '["reimbursement:self"]'
                WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "RoleName" = 'Employee');
                INSERT INTO "Roles" ("RoleName", "ApprovalLimit", "CanApprove", "CanManageRoles", "CanProcessPayments", "PermissionsJson")
                SELECT 'Manager', 100000, true, false, false, '["reimbursement:approve"]'
                WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "RoleName" = 'Manager');
                INSERT INTO "Roles" ("RoleName", "ApprovalLimit", "CanApprove", "CanManageRoles", "CanProcessPayments", "PermissionsJson")
                SELECT 'DepartmentHead', 1000000, true, false, false, '["reimbursement:approve"]'
                WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "RoleName" = 'DepartmentHead');
                INSERT INTO "Roles" ("RoleName", "ApprovalLimit", "CanApprove", "CanManageRoles", "CanProcessPayments", "PermissionsJson")
                SELECT 'Finance', 0, false, false, true, '["payment:process"]'
                WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "RoleName" = 'Finance');
                INSERT INTO "Roles" ("RoleName", "ApprovalLimit", "CanApprove", "CanManageRoles", "CanProcessPayments", "PermissionsJson")
                SELECT 'Admin', 9999999999999999.99, true, true, true, '["*"]'
                WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "RoleName" = 'Admin');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reimbursements_IdempotencyKey",
                table: "Reimbursements",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalProcesses_ApprovalWorkflowTemplateId",
                table: "ApprovalProcesses",
                column: "ApprovalWorkflowTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalProcesses_ReimbursementId",
                table: "ApprovalProcesses",
                column: "ReimbursementId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStageDefinitions_ApprovalWorkflowTemplateId_Sequence",
                table: "ApprovalStageDefinitions",
                columns: new[] { "ApprovalWorkflowTemplateId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_ApprovalProcessId_Sequence",
                table: "ApprovalSteps",
                columns: new[] { "ApprovalProcessId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_DecidedByEmployeeId",
                table: "ApprovalSteps",
                column: "DecidedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowTemplates_DepartmentId",
                table: "ApprovalWorkflowTemplates",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EmployeeId",
                table: "AuditLogs",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_IdempotencyKey",
                table: "PaymentTransactions",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_ReimbursementId",
                table: "PaymentTransactions",
                column: "ReimbursementId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolExecutions_WorkflowStepId",
                table: "ToolExecutions",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationResults_WorkflowStepId",
                table: "ValidationResults",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_ExpenseClaimId",
                table: "WorkflowExecutions",
                column: "ExpenseClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSteps_WorkflowExecutionId",
                table: "WorkflowSteps",
                column: "WorkflowExecutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalStageDefinitions");

            migrationBuilder.DropTable(
                name: "ApprovalSteps");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "ToolExecutions");

            migrationBuilder.DropTable(
                name: "ValidationResults");

            migrationBuilder.DropTable(
                name: "ApprovalProcesses");

            migrationBuilder.DropTable(
                name: "WorkflowSteps");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflowTemplates");

            migrationBuilder.DropTable(
                name: "WorkflowExecutions");

            migrationBuilder.DropIndex(
                name: "IX_Roles_RoleName",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Reimbursements_IdempotencyKey",
                table: "Reimbursements");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "CanManageRoles",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CanProcessPayments",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "PermissionsJson",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Reimbursements");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Reimbursements");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "Reimbursements");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Reimbursements");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "Reimbursements");

            migrationBuilder.DropColumn(
                name: "PaymentProvider",
                table: "Reimbursements");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "Reimbursements");

            migrationBuilder.DropColumn(
                name: "RequestedAt",
                table: "Reimbursements");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "Reimbursements");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Employees");
        }
    }
}
