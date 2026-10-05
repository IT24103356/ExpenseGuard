using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReimbursementBudget.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepartmentBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DepartmentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    BudgetPeriod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ApprovedSpend = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaidSpend = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentBudgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reimbursements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<string>(type: "text", nullable: false),
                    DepartmentId = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaymentProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reimbursements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpendSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TotalSpend = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BudgetAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Variance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UtilizationPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpendSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpenseClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    Objective = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrentStep = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PlanJson = table.Column<string>(type: "text", nullable: true),
                    AgentOutputsJson = table.Column<string>(type: "text", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "text", nullable: true),
                    ApproverId = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovalComment = table.Column<string>(type: "text", nullable: true),
                    PaymentStatus = table.Column<string>(type: "text", nullable: true),
                    FinalOutcome = table.Column<string>(type: "text", nullable: true),
                    TotalSteps = table.Column<int>(type: "integer", nullable: false),
                    CompletedSteps = table.Column<int>(type: "integer", nullable: false),
                    MaxRetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxStepCount = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BudgetAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DepartmentBudgetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ThresholdPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CurrentSpend = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BudgetAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetAlerts_DepartmentBudgets_DepartmentBudgetId",
                        column: x => x.DepartmentBudgetId,
                        principalTable: "DepartmentBudgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BudgetTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentBudgetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseClaimId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReimbursementId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetTransactions_DepartmentBudgets_DepartmentBudgetId",
                        column: x => x.DepartmentBudgetId,
                        principalTable: "DepartmentBudgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetTransactions_Reimbursements_ReimbursementId",
                        column: x => x.ReimbursementId,
                        principalTable: "Reimbursements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReimbursementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalTransactionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestPayloadSummary = table.Column<string>(type: "text", nullable: true),
                    ResponseSummary = table.Column<string>(type: "text", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Reimbursements_ReimbursementId",
                        column: x => x.ReimbursementId,
                        principalTable: "Reimbursements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    StepName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AgentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StepType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InputReference = table.Column<string>(type: "text", nullable: true),
                    OutputReference = table.Column<string>(type: "text", nullable: true),
                    ValidationResult = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowSteps_WorkflowExecutions_WorkflowExecutionId",
                        column: x => x.WorkflowExecutionId,
                        principalTable: "WorkflowExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetAlerts_DepartmentBudgetId",
                table: "BudgetAlerts",
                column: "DepartmentBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_DepartmentBudgetId",
                table: "BudgetTransactions",
                column: "DepartmentBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_ReimbursementId",
                table: "BudgetTransactions",
                column: "ReimbursementId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_TransactionDate",
                table: "BudgetTransactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentBudgets_DepartmentId_FiscalYear_BudgetPeriod",
                table: "DepartmentBudgets",
                columns: new[] { "DepartmentId", "FiscalYear", "BudgetPeriod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_ExternalTransactionId",
                table: "PaymentTransactions",
                column: "ExternalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_ReimbursementId",
                table: "PaymentTransactions",
                column: "ReimbursementId");

            migrationBuilder.CreateIndex(
                name: "IX_Reimbursements_DepartmentId",
                table: "Reimbursements",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Reimbursements_ExpenseClaimId",
                table: "Reimbursements",
                column: "ExpenseClaimId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reimbursements_IdempotencyKey",
                table: "Reimbursements",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reimbursements_Status",
                table: "Reimbursements",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SpendSummaries_DepartmentId_Period_Category",
                table: "SpendSummaries",
                columns: new[] { "DepartmentId", "Period", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_CorrelationId",
                table: "WorkflowExecutions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_ExpenseClaimId",
                table: "WorkflowExecutions",
                column: "ExpenseClaimId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_Status",
                table: "WorkflowExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_WorkflowId",
                table: "WorkflowExecutions",
                column: "WorkflowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSteps_WorkflowExecutionId_StepNumber",
                table: "WorkflowSteps",
                columns: new[] { "WorkflowExecutionId", "StepNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetAlerts");

            migrationBuilder.DropTable(
                name: "BudgetTransactions");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "SpendSummaries");

            migrationBuilder.DropTable(
                name: "WorkflowSteps");

            migrationBuilder.DropTable(
                name: "DepartmentBudgets");

            migrationBuilder.DropTable(
                name: "Reimbursements");

            migrationBuilder.DropTable(
                name: "WorkflowExecutions");
        }
    }
}
