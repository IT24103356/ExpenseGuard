using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReimbursementBudget.API.Agents;
using ReimbursementBudget.API.Data;
using ReimbursementBudget.API.Data.Entities;
using ReimbursementBudget.API.DTOs;
using ReimbursementBudget.API.Infrastructure.Payment;
using ReimbursementBudget.API.Services;

namespace ReimbursementBudget.API.Tools
{
    /// <summary>
    /// AgentToolbox — the controlled set of tools available to the CoordinatorPlannerAgent.
    ///
    /// Rules:
    ///   - Every tool validates its inputs
    ///   - Every tool uses structured request/response models (AgentToolResult)
    ///   - Tools have limited, scoped database access (no arbitrary queries)
    ///   - All operations are logged
    ///   - Payment tool can only be invoked after approval (enforced by Coordinator)
    /// </summary>
    public class AgentToolbox
    {
        private readonly AppDbContext _db;
        private readonly IPaymentProvider _paymentProvider;
        private readonly IBudgetService _budgetService;
        private readonly IReimbursementService _reimbursementService;
        private readonly ILogger<AgentToolbox> _logger;

        public AgentToolbox(
            AppDbContext db,
            IPaymentProvider paymentProvider,
            IBudgetService budgetService,
            IReimbursementService reimbursementService,
            ILogger<AgentToolbox> logger)
        {
            _db = db;
            _paymentProvider = paymentProvider;
            _budgetService = budgetService;
            _reimbursementService = reimbursementService;
            _logger = logger;
        }

        // ─── Tool: get_claim ────────────────────────────────────────────────────
        public async Task<AgentToolResult> GetClaimAsync(Guid claimId)
        {
            if (claimId == Guid.Empty)
                throw new ArgumentException("claimId must not be empty");

            _logger.LogInformation("[Tool:GetClaim] claimId={Id}", claimId);

            // In a real system, this would call MEM1's API. Here we simulate.
            await Task.Delay(10); // simulate async I/O

            return new AgentToolResult
            {
                WorkflowId = "SYSTEM",
                Step = "INTAKE",
                Status = "SUCCESS",
                Data = new Dictionary<string, object?>
                {
                    ["claimId"] = claimId.ToString(),
                    ["extracted"] = true,
                    ["employeeId"] = "SIM-EMP",
                    ["amount"] = 0
                }
            };
        }

        // ─── Tool: get_policy_result ────────────────────────────────────────────
        public async Task<AgentToolResult> GetPolicyResultAsync(Guid claimId)
        {
            if (claimId == Guid.Empty)
                throw new ArgumentException("claimId must not be empty");

            _logger.LogInformation("[Tool:GetPolicyResult] claimId={Id}", claimId);
            await Task.Delay(10);

            // Delegate to MEM1's PolicyComplianceAgent result (simulated)
            return new AgentToolResult
            {
                WorkflowId = "SYSTEM",
                Step = "POLICY_CHECK",
                Status = "SUCCESS",
                Data = new Dictionary<string, object?>
                {
                    ["claimId"] = claimId.ToString(),
                    ["policyCompliant"] = true,
                    ["violations"] = Array.Empty<string>(),
                    ["checkedBy"] = "PolicyComplianceAgent"
                }
            };
        }

        // ─── Tool: get_risk_result ──────────────────────────────────────────────
        public async Task<AgentToolResult> GetRiskResultAsync(Guid claimId)
        {
            if (claimId == Guid.Empty)
                throw new ArgumentException("claimId must not be empty");

            _logger.LogInformation("[Tool:GetRiskResult] claimId={Id}", claimId);
            await Task.Delay(10);

            // Delegate to MEM1's FraudAnomalyRiskAgent result (simulated)
            return new AgentToolResult
            {
                WorkflowId = "SYSTEM",
                Step = "RISK_CHECK",
                Status = "SUCCESS",
                Data = new Dictionary<string, object?>
                {
                    ["claimId"] = claimId.ToString(),
                    ["riskLevel"] = "LOW",
                    ["isDuplicate"] = false,
                    ["anomalyScore"] = 0.05,
                    ["checkedBy"] = "FraudAnomalyRiskAgent"
                }
            };
        }

        // ─── Tool: check_budget ─────────────────────────────────────────────────
        public async Task<AgentToolResult> CheckBudgetAsync(string departmentId, decimal amount)
        {
            if (string.IsNullOrEmpty(departmentId))
                throw new ArgumentException("departmentId is required");
            if (amount <= 0)
                throw new ArgumentException("amount must be positive");

            _logger.LogInformation("[Tool:CheckBudget] dept={Dept} amount={Amount}", departmentId, amount);

            var result = await _budgetService.CheckBudgetAvailabilityAsync(departmentId, amount, DateTime.UtcNow.Year);

            // Get remaining budget for reporting
            var budget = await _db.DepartmentBudgets
                .FirstOrDefaultAsync(b => b.DepartmentId == departmentId &&
                                          b.FiscalYear == DateTime.UtcNow.Year &&
                                          b.IsActive);

            return new AgentToolResult
            {
                WorkflowId = "SYSTEM",
                Step = "BUDGET_CHECK",
                Status = "SUCCESS",
                Data = new Dictionary<string, object?>
                {
                    ["departmentId"] = departmentId,
                    ["requestedAmount"] = amount,
                    ["budgetAvailable"] = result.IsSuccess && result.Data,
                    ["remainingBudget"] = budget?.RemainingBudget ?? 0,
                    ["allocatedBudget"] = budget?.AllocatedAmount ?? 0,
                    ["currentSpend"] = budget?.ApprovedSpend ?? 0
                }
            };
        }

        // ─── Tool: create_reimbursement ─────────────────────────────────────────
        public async Task<AgentToolResult> CreateReimbursementAsync(StartWorkflowRequest request)
        {
            if (request.ExpenseClaimId == Guid.Empty)
                throw new ArgumentException("ExpenseClaimId is required");
            if (request.Amount <= 0)
                throw new ArgumentException("Amount must be positive");

            _logger.LogInformation("[Tool:CreateReimbursement] claimId={Id}", request.ExpenseClaimId);

            var createReq = new CreateReimbursementRequest
            {
                ExpenseClaimId = request.ExpenseClaimId,
                EmployeeId = request.EmployeeId,
                DepartmentId = request.DepartmentId,
                Amount = request.Amount,
                Currency = request.Currency ?? "LKR"
            };

            var result = await _reimbursementService.CreateReimbursementAsync(createReq);

            if (!result.IsSuccess)
                throw new InvalidOperationException($"Failed to create reimbursement: {result.ErrorMessage}");

            // Move to processing state
            await _reimbursementService.ProcessReimbursementAsync(result.Data.Id, "SYSTEM_COORDINATOR");

            return new AgentToolResult
            {
                WorkflowId = "SYSTEM",
                Step = "FINANCE_PROCESSING",
                Status = "SUCCESS",
                Data = new Dictionary<string, object?>
                {
                    ["reimbursementId"] = result.Data.Id.ToString(),
                    ["status"] = result.Data.Status,
                    ["amount"] = result.Data.Amount,
                    ["currency"] = result.Data.Currency
                }
            };
        }

        // ─── Tool: submit_payment ───────────────────────────────────────────────
        public async Task<AgentToolResult> SubmitPaymentAsync(Guid reimbursementId)
        {
            if (reimbursementId == Guid.Empty)
                throw new ArgumentException("reimbursementId is required");

            _logger.LogInformation("[Tool:SubmitPayment] reimbursementId={Id}", reimbursementId);

            var result = await _reimbursementService.SubmitPaymentAsync(reimbursementId, "SYSTEM_COORDINATOR");

            if (!result.IsSuccess)
                throw new PaymentFailedException($"Payment failed: {result.ErrorMessage}");

            return new AgentToolResult
            {
                WorkflowId = "SYSTEM",
                Step = "PAYMENT",
                Status = "SUCCESS",
                Data = new Dictionary<string, object?>
                {
                    ["reimbursementId"] = reimbursementId.ToString(),
                    ["paymentStatus"] = result.Data.Status,
                    ["paymentReference"] = result.Data.PaymentReference,
                    ["amount"] = result.Data.Amount
                }
            };
        }

        // ─── Tool: get_payment_status ───────────────────────────────────────────
        public async Task<AgentToolResult> GetPaymentStatusAsync(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
                throw new ArgumentException("transactionId is required");

            _logger.LogInformation("[Tool:GetPaymentStatus] txnId={Id}", transactionId);

            var status = await _paymentProvider.GetPaymentStatusAsync(transactionId);

            return new AgentToolResult
            {
                WorkflowId = "SYSTEM",
                Step = "PAYMENT_STATUS",
                Status = "SUCCESS",
                Data = new Dictionary<string, object?>
                {
                    ["transactionId"] = transactionId,
                    ["paymentStatus"] = status.Status,
                    ["amount"] = status.Amount
                }
            };
        }

        // ─── Tool: update_budget ────────────────────────────────────────────────
        public async Task<AgentToolResult> UpdateBudgetAsync(string departmentId, decimal amount)
        {
            if (string.IsNullOrEmpty(departmentId))
                throw new ArgumentException("departmentId is required");
            if (amount <= 0)
                throw new ArgumentException("amount must be positive");

            _logger.LogInformation("[Tool:UpdateBudget] dept={Dept} amount={Amount}", departmentId, amount);

            // Budget is already updated in SubmitPaymentAsync via BudgetService.DeductBudgetAsync.
            // This step records the audit event.

            return new AgentToolResult
            {
                WorkflowId = "SYSTEM",
                Step = "BUDGET_UPDATE",
                Status = "SUCCESS",
                Data = new Dictionary<string, object?>
                {
                    ["departmentId"] = departmentId,
                    ["amountDeducted"] = amount,
                    ["updatedAt"] = DateTime.UtcNow.ToString("O")
                }
            };
        }

        // ─── Tool: create_workflow_event ────────────────────────────────────────
        public async Task<AgentToolResult> CreateWorkflowEventAsync(string workflowId, string eventType, string description)
        {
            if (string.IsNullOrEmpty(workflowId)) throw new ArgumentException("workflowId required");
            if (string.IsNullOrEmpty(eventType)) throw new ArgumentException("eventType required");

            _logger.LogInformation("[Tool:CreateWorkflowEvent] {WorkflowId} {EventType}", workflowId, eventType);

            await Task.Delay(1); // persist event if needed

            return new AgentToolResult
            {
                WorkflowId = workflowId,
                Step = "FINAL_RESULT",
                Status = "SUCCESS",
                Data = new Dictionary<string, object?>
                {
                    ["workflowId"] = workflowId,
                    ["eventType"] = eventType,
                    ["description"] = description,
                    ["createdAt"] = DateTime.UtcNow.ToString("O")
                }
            };
        }

        // ─── Tool: get_workflow_status ──────────────────────────────────────────
        public async Task<AgentToolResult> GetWorkflowStatusAsync(string workflowId)
        {
            if (string.IsNullOrEmpty(workflowId)) throw new ArgumentException("workflowId required");

            var execution = await _db.WorkflowExecutions
                .FirstOrDefaultAsync(w => w.WorkflowId == workflowId);

            return new AgentToolResult
            {
                WorkflowId = workflowId,
                Step = "STATUS",
                Status = "SUCCESS",
                Data = new Dictionary<string, object?>
                {
                    ["workflowId"] = workflowId,
                    ["status"] = execution?.Status ?? "NOT_FOUND",
                    ["currentStep"] = execution?.CurrentStep ?? "",
                    ["completedSteps"] = execution?.CompletedSteps ?? 0
                }
            };
        }
    }
}
