using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReimbursementBudget.API.Data;
using ReimbursementBudget.API.Data.Entities;
using ReimbursementBudget.API.DTOs;
using ReimbursementBudget.API.Tools;

namespace ReimbursementBudget.API.Agents
{
    /// <summary>
    /// CoordinatorPlannerAgent — the orchestration agent for the Reimbursement workflow.
    ///
    /// Responsibilities:
    ///   1. Create a structured execution plan
    ///   2. Validate the plan against an allow-list (no arbitrary agents/tools)
    ///   3. Delegate each step to the appropriate specialized agent / tool
    ///   4. Persist full workflow state in PostgreSQL after every step
    ///   5. Stop at HUMAN_APPROVAL boundaries — never skip manager review
    ///   6. Handle errors, timeouts, and retries with limits
    ///   7. Prevent duplicate workflows and duplicate payments (idempotency)
    /// </summary>
    public class CoordinatorPlannerAgent
    {
        // ── Allow-listed agents & tools ──────────────────────────────────────────
        private static readonly HashSet<string> AllowedAgents = new(StringComparer.OrdinalIgnoreCase)
        {
            "ExpenseExtractionAgent",
            "PolicyComplianceAgent",
            "FraudAnomalyRiskAgent",
            "FinanceAgent",
            "BudgetAgent"
        };

        private static readonly HashSet<string> AllowedTools = new(StringComparer.OrdinalIgnoreCase)
        {
            "PaymentSandbox"
        };

        private const int MaxStepCount = 20;
        private const int MaxRetryCount = 3;

        // ── Dependencies ─────────────────────────────────────────────────────────
        private readonly AppDbContext _db;
        private readonly AgentToolbox _tools;
        private readonly ILogger<CoordinatorPlannerAgent> _logger;

        public CoordinatorPlannerAgent(
            AppDbContext db,
            AgentToolbox tools,
            ILogger<CoordinatorPlannerAgent> logger)
        {
            _db = db;
            _tools = tools;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>Starts a new workflow for the given expense claim. Idempotent.</summary>
        public async Task<WorkflowExecutionDto> StartWorkflowAsync(StartWorkflowRequest request)
        {
            _logger.LogInformation("[Coordinator] StartWorkflow ClaimId={ClaimId}", request.ExpenseClaimId);

            // Idempotency: one workflow per claim
            var existing = await _db.WorkflowExecutions
                .Include(w => w.Steps)
                .FirstOrDefaultAsync(w => w.ExpenseClaimId == request.ExpenseClaimId);

            if (existing != null)
            {
                _logger.LogWarning("[Coordinator] Duplicate workflow blocked for ClaimId={ClaimId}", request.ExpenseClaimId);
                return MapToDto(existing);
            }

            // Generate workflow ID
            var counter = await _db.WorkflowExecutions.CountAsync() + 1;
            var workflowId = $"WF-{10000 + counter}";

            // Build plan
            var plan = BuildPlan(workflowId, request);
            ValidatePlan(plan); // throws if invalid

            var execution = new WorkflowExecution
            {
                WorkflowId = workflowId,
                ExpenseClaimId = request.ExpenseClaimId,
                Objective = $"Process expense reimbursement for Employee {request.EmployeeId} — Amount {request.Amount} {request.Currency ?? "LKR"}",
                Status = WorkflowStatus.Created,
                CurrentStep = plan[0].StepName,
                PlanJson = JsonSerializer.Serialize(plan),
                TotalSteps = plan.Count,
                MaxRetryCount = MaxRetryCount,
                MaxStepCount = MaxStepCount
            };

            // Persist planned steps
            foreach (var step in plan)
            {
                execution.Steps.Add(new WorkflowStep
                {
                    StepNumber = step.StepNumber,
                    StepName = step.StepName,
                    AgentName = step.Agent ?? step.Tool ?? step.Type ?? "SYSTEM",
                    StepType = step.Type ?? (step.Tool != null ? "TOOL" : "AGENT"),
                    Status = WorkflowStepStatus.Pending,
                    InputReference = JsonSerializer.Serialize(new { step.Action })
                });
            }

            _db.WorkflowExecutions.Add(execution);
            await _db.SaveChangesAsync();

            _logger.LogInformation("[Coordinator] Created workflow {WorkflowId} with {Steps} steps", workflowId, plan.Count);

            // Start executing immediately (async)
            _ = ExecuteWorkflowAsync(execution.Id, request);

            return MapToDto(execution);
        }

        /// <summary>Resumes a workflow after human approval decision.</summary>
        public async Task<WorkflowExecutionDto> ResumeAfterApprovalAsync(Guid workflowId, ApprovalDecisionRequest decision)
        {
            var execution = await _db.WorkflowExecutions
                .Include(w => w.Steps)
                .FirstOrDefaultAsync(w => w.Id == workflowId);

            if (execution == null)
                throw new InvalidOperationException($"Workflow {workflowId} not found");

            if (execution.Status != WorkflowStatus.WaitingForApproval)
                throw new InvalidOperationException($"Workflow is not awaiting approval. Current status: {execution.Status}");

            _logger.LogInformation("[Coordinator] ApprovalDecision={Decision} WorkflowId={Id}", decision.Decision, workflowId);

            execution.ApprovalStatus = decision.Decision;
            execution.ApproverId = decision.ApproverId;
            execution.ApprovedAt = DateTime.UtcNow;
            execution.ApprovalComment = decision.Comment;
            execution.UpdatedAt = DateTime.UtcNow;

            // Mark the human approval step as completed
            var approvalStep = execution.Steps
                .FirstOrDefault(s => s.StepType == "HUMAN_APPROVAL" && s.Status == WorkflowStepStatus.WaitingForHuman);

            if (approvalStep != null)
            {
                approvalStep.Status = WorkflowStepStatus.Completed;
                approvalStep.CompletedAt = DateTime.UtcNow;
                approvalStep.OutputReference = JsonSerializer.Serialize(decision);
            }

            switch (decision.Decision.ToUpper())
            {
                case "APPROVED":
                    execution.Status = WorkflowStatus.Approved;
                    break;

                case "REJECTED":
                    execution.Status = WorkflowStatus.Rejected;
                    execution.FinalOutcome = $"REJECTED by {decision.ApproverId}: {decision.Comment}";
                    await _db.SaveChangesAsync();
                    _logger.LogWarning("[Coordinator] Workflow REJECTED. Payment tool will NOT be called.");
                    return MapToDto(execution);

                case "REVISION_REQUIRED":
                    execution.Status = WorkflowStatus.RevisionRequired;
                    execution.FinalOutcome = $"REVISION_REQUIRED: {decision.Comment}";
                    await _db.SaveChangesAsync();
                    return MapToDto(execution);

                default:
                    throw new InvalidOperationException($"Unknown decision: {decision.Decision}");
            }

            await _db.SaveChangesAsync();

            // Deserialize original request context from plan for resumption
            var originalRequest = DeserializeRequestFromPlan(execution);
            _ = ContinueWorkflowAfterApprovalAsync(execution.Id, originalRequest);

            return MapToDto(execution);
        }

        /// <summary>Gets workflow status by execution ID.</summary>
        public async Task<WorkflowExecutionDto?> GetWorkflowAsync(Guid id)
        {
            var w = await _db.WorkflowExecutions
                .Include(x => x.Steps)
                .FirstOrDefaultAsync(x => x.Id == id);
            return w == null ? null : MapToDto(w);
        }

        /// <summary>Gets workflow by claim ID.</summary>
        public async Task<WorkflowExecutionDto?> GetWorkflowByClaimAsync(Guid claimId)
        {
            var w = await _db.WorkflowExecutions
                .Include(x => x.Steps)
                .FirstOrDefaultAsync(x => x.ExpenseClaimId == claimId);
            return w == null ? null : MapToDto(w);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // PLAN GENERATION
        // ─────────────────────────────────────────────────────────────────────────

        private List<PlannedStep> BuildPlan(string workflowId, StartWorkflowRequest request)
        {
            return new List<PlannedStep>
            {
                new() { StepNumber = 1, StepName = "INTAKE",       Agent = "ExpenseExtractionAgent",  Action = "Extract and validate claim information" },
                new() { StepNumber = 2, StepName = "POLICY_CHECK", Agent = "PolicyComplianceAgent",   Action = "Validate against expense policies and limits" },
                new() { StepNumber = 3, StepName = "RISK_CHECK",   Agent = "FraudAnomalyRiskAgent",   Action = "Check for duplicates, anomalies, and risk level" },
                new() { StepNumber = 4, StepName = "HUMAN_APPROVAL", Type = "HUMAN_APPROVAL",         Action = "Manager review and approval decision" },
                new() { StepNumber = 5, StepName = "BUDGET_CHECK", Agent = "BudgetAgent",             Action = "Check department budget availability" },
                new() { StepNumber = 6, StepName = "FINANCE_PROCESSING", Agent = "FinanceAgent",      Action = "Prepare reimbursement record and queue" },
                new() { StepNumber = 7, StepName = "PAYMENT",      Tool = "PaymentSandbox",           Action = "Submit payment to sandbox provider" },
                new() { StepNumber = 8, StepName = "BUDGET_UPDATE", Agent = "BudgetAgent",            Action = "Deduct from department budget and record transaction" },
                new() { StepNumber = 9, StepName = "FINAL_RESULT", Agent = "FinanceAgent",            Action = "Update employee status and create audit event" }
            };
        }

        /// <summary>Validates that the plan only uses allow-listed agents and tools.</summary>
        private static void ValidatePlan(List<PlannedStep> plan)
        {
            if (plan.Count > MaxStepCount)
                throw new InvalidOperationException($"Plan exceeds maximum step count ({MaxStepCount})");

            foreach (var step in plan)
            {
                if (step.Agent != null && !AllowedAgents.Contains(step.Agent))
                    throw new InvalidOperationException($"Agent '{step.Agent}' is not in the allow-list");
                if (step.Tool != null && !AllowedTools.Contains(step.Tool))
                    throw new InvalidOperationException($"Tool '{step.Tool}' is not in the allow-list");
                if (step.Type != null && step.Type != "HUMAN_APPROVAL" && step.Type != "AGENT" && step.Type != "TOOL")
                    throw new InvalidOperationException($"Step type '{step.Type}' is not valid");
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // EXECUTION ENGINE
        // ─────────────────────────────────────────────────────────────────────────

        private async Task ExecuteWorkflowAsync(Guid executionId, StartWorkflowRequest request)
        {
            var execution = await _db.WorkflowExecutions
                .Include(w => w.Steps)
                .FirstOrDefaultAsync(w => w.Id == executionId);

            if (execution == null) return;

            try
            {
                execution.Status = WorkflowStatus.Planned;
                await _db.SaveChangesAsync();

                // Step 1: Intake / Extraction
                await ExecuteStepAsync(execution, 1, async step =>
                {
                    execution.Status = WorkflowStatus.Extracting;
                    var result = await _tools.GetClaimAsync(request.ExpenseClaimId);
                    return result;
                });

                // Step 2: Policy Check
                await ExecuteStepAsync(execution, 2, async step =>
                {
                    execution.Status = WorkflowStatus.PolicyChecking;
                    var result = await _tools.GetPolicyResultAsync(request.ExpenseClaimId);
                    // Validate output schema
                    ValidateAgentOutput(result, "POLICY_CHECK");
                    return result;
                });

                // Step 3: Risk/Fraud Check
                await ExecuteStepAsync(execution, 3, async step =>
                {
                    execution.Status = WorkflowStatus.RiskChecking;
                    var result = await _tools.GetRiskResultAsync(request.ExpenseClaimId);
                    ValidateAgentOutput(result, "RISK_CHECK");
                    return result;
                });

                // Step 4: HUMAN APPROVAL PAUSE — coordinator stops here
                var approvalStep = execution.Steps.First(s => s.StepNumber == 4);
                approvalStep.Status = WorkflowStepStatus.WaitingForHuman;
                approvalStep.StartedAt = DateTime.UtcNow;
                execution.Status = WorkflowStatus.WaitingForApproval;
                execution.CurrentStep = "HUMAN_APPROVAL";
                execution.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("[Coordinator] Paused at HUMAN_APPROVAL for workflow {Id}. " +
                    "Payment tool will NOT be called until manager approves.", executionId);

                // Do NOT call submit_payment() here. The workflow resumes via ResumeAfterApprovalAsync().
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Coordinator] Workflow {Id} failed at step {Step}", executionId, execution.CurrentStep);
                execution.Status = WorkflowStatus.Failed;
                execution.FinalOutcome = $"FAILED: {ex.Message}";
                execution.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        private async Task ContinueWorkflowAfterApprovalAsync(Guid executionId, StartWorkflowRequest? request)
        {
            var execution = await _db.WorkflowExecutions
                .Include(w => w.Steps)
                .FirstOrDefaultAsync(w => w.Id == executionId);

            if (execution == null || request == null) return;

            try
            {
                // Step 5: Budget Check
                await ExecuteStepAsync(execution, 5, async step =>
                {
                    execution.Status = WorkflowStatus.BudgetChecking;
                    var result = await _tools.CheckBudgetAsync(request.DepartmentId, request.Amount);
                    ValidateAgentOutput(result, "BUDGET_CHECK");

                    // If budget insufficient, pause
                    if (result?.Data?.ContainsKey("budgetAvailable") == true &&
                        result.Data["budgetAvailable"]?.ToString() == "False")
                    {
                        execution.Status = WorkflowStatus.BudgetReviewRequired;
                        execution.FinalOutcome = "BUDGET_REVIEW_REQUIRED: Insufficient department budget";
                        execution.UpdatedAt = DateTime.UtcNow;
                        throw new BudgetInsufficientException("Insufficient budget. Workflow paused for review.");
                    }

                    return result;
                });

                // Step 6: Finance Processing
                await ExecuteStepAsync(execution, 6, async step =>
                {
                    execution.Status = WorkflowStatus.FinanceProcessing;
                    var result = await _tools.CreateReimbursementAsync(request);
                    ValidateAgentOutput(result, "FINANCE_PROCESSING");
                    return result;
                });

                // Step 7: Payment (TOOL — only called after approval + budget check)
                await ExecuteStepAsync(execution, 7, async step =>
                {
                    execution.Status = WorkflowStatus.PaymentProcessing;
                    var reimbResult = DeserializeReimbursementFromOutputs(execution);
                    var result = await _tools.SubmitPaymentAsync(reimbResult);
                    ValidateAgentOutput(result, "PAYMENT");

                    if (result?.Data?.ContainsKey("paymentStatus") == true &&
                        result.Data["paymentStatus"]?.ToString() != "COMPLETED")
                    {
                        throw new PaymentFailedException($"Payment failed: {result.Data.GetValueOrDefault("failureReason")}");
                    }

                    return result;
                });

                // Step 8: Budget Update
                await ExecuteStepAsync(execution, 8, async step =>
                {
                    var result = await _tools.UpdateBudgetAsync(request.DepartmentId, request.Amount);
                    ValidateAgentOutput(result, "BUDGET_UPDATE");
                    return result;
                });

                // Step 9: Final Result
                await ExecuteStepAsync(execution, 9, async step =>
                {
                    var result = await _tools.CreateWorkflowEventAsync(execution.WorkflowId, "COMPLETED",
                        $"Reimbursement completed for {request.EmployeeId}");
                    return result;
                });

                execution.Status = WorkflowStatus.Completed;
                execution.CompletedSteps = execution.Steps.Count(s => s.Status == WorkflowStepStatus.Completed);
                execution.FinalOutcome = "COMPLETED: Expense reimbursed and budget updated successfully";
                execution.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("[Coordinator] Workflow {Id} COMPLETED successfully", executionId);
            }
            catch (BudgetInsufficientException ex)
            {
                _logger.LogWarning("[Coordinator] Workflow {Id} paused: {Msg}", executionId, ex.Message);
                // Status already set to BudgetReviewRequired above
                await _db.SaveChangesAsync();
            }
            catch (PaymentFailedException ex)
            {
                _logger.LogError("[Coordinator] Workflow {Id} PAYMENT_FAILED: {Msg}", executionId, ex.Message);
                execution.Status = WorkflowStatus.Failed;
                execution.FinalOutcome = $"PAYMENT_FAILED: {ex.Message}";
                execution.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Coordinator] Workflow {Id} failed", executionId);
                execution.Status = WorkflowStatus.Failed;
                execution.FinalOutcome = $"FAILED: {ex.Message}";
                execution.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // STEP EXECUTION HELPER
        // ─────────────────────────────────────────────────────────────────────────

        private async Task ExecuteStepAsync(
            WorkflowExecution execution,
            int stepNumber,
            Func<WorkflowStep, Task<AgentToolResult?>> action)
        {
            var step = execution.Steps.FirstOrDefault(s => s.StepNumber == stepNumber);
            if (step == null || step.Status == WorkflowStepStatus.Completed) return;

            step.Status = WorkflowStepStatus.InProgress;
            step.StartedAt = DateTime.UtcNow;
            execution.CurrentStep = step.StepName;
            execution.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            try
            {
                var result = await action(step);
                step.OutputReference = result != null ? JsonSerializer.Serialize(result) : null;
                step.ValidationResult = "VALID";
                step.Status = WorkflowStepStatus.Completed;
                step.CompletedAt = DateTime.UtcNow;
                execution.CompletedSteps++;
            }
            catch (Exception ex) when (step.RetryCount < MaxRetryCount &&
                                        !(ex is BudgetInsufficientException) &&
                                        !(ex is PaymentFailedException))
            {
                step.RetryCount++;
                step.ErrorMessage = $"Retry {step.RetryCount}: {ex.Message}";
                _logger.LogWarning("[Coordinator] Step {Step} failed, retry {Count}/{Max}", stepNumber, step.RetryCount, MaxRetryCount);
                await _db.SaveChangesAsync();
                await Task.Delay(500 * step.RetryCount); // exponential-ish backoff
                await ExecuteStepAsync(execution, stepNumber, action);
                return;
            }
            catch (Exception ex)
            {
                step.Status = WorkflowStepStatus.Failed;
                step.ErrorMessage = ex.Message;
                step.CompletedAt = DateTime.UtcNow;
                throw;
            }
            finally
            {
                await _db.SaveChangesAsync();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // OUTPUT VALIDATION
        // ─────────────────────────────────────────────────────────────────────────

        private static void ValidateAgentOutput(AgentToolResult? result, string expectedStep)
        {
            if (result == null)
                throw new InvalidOperationException($"Agent output for {expectedStep} was null");
            if (string.IsNullOrEmpty(result.WorkflowId))
                throw new InvalidOperationException($"Agent output missing workflowId for step {expectedStep}");
            if (string.IsNullOrEmpty(result.Status))
                throw new InvalidOperationException($"Agent output missing status for step {expectedStep}");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────────

        private StartWorkflowRequest? DeserializeRequestFromPlan(WorkflowExecution execution)
        {
            try
            {
                if (execution.AgentOutputsJson == null) return null;
                return JsonSerializer.Deserialize<StartWorkflowRequest>(execution.AgentOutputsJson);
            }
            catch { return null; }
        }

        private Guid DeserializeReimbursementFromOutputs(WorkflowExecution execution)
        {
            // Try to extract reimbursement ID from step 6 output
            var financeStep = execution.Steps.FirstOrDefault(s => s.StepNumber == 6);
            if (financeStep?.OutputReference != null)
            {
                try
                {
                    var result = JsonSerializer.Deserialize<AgentToolResult>(financeStep.OutputReference);
                    if (result?.Data?.TryGetValue("reimbursementId", out var idVal) == true &&
                        Guid.TryParse(idVal?.ToString(), out var rid))
                        return rid;
                }
                catch { /* fall through */ }
            }
            return Guid.Empty;
        }

        private static WorkflowExecutionDto MapToDto(WorkflowExecution w) => new()
        {
            Id = w.Id,
            WorkflowId = w.WorkflowId,
            ExpenseClaimId = w.ExpenseClaimId,
            Objective = w.Objective,
            Status = w.Status,
            CurrentStep = w.CurrentStep,
            ApprovalStatus = w.ApprovalStatus,
            FinalOutcome = w.FinalOutcome,
            TotalSteps = w.TotalSteps,
            CompletedSteps = w.CompletedSteps,
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt,
            Steps = w.Steps
                .OrderBy(s => s.StepNumber)
                .Select(s => new WorkflowStepDto
                {
                    Id = s.Id,
                    StepNumber = s.StepNumber,
                    StepName = s.StepName,
                    AgentName = s.AgentName,
                    Status = s.Status,
                    StepType = s.StepType,
                    ValidationResult = s.ValidationResult,
                    ErrorMessage = s.ErrorMessage,
                    RetryCount = s.RetryCount,
                    StartedAt = s.StartedAt,
                    CompletedAt = s.CompletedAt
                }).ToList()
        };
    }

    // ─── Supporting types ────────────────────────────────────────────────────────

    public class PlannedStep
    {
        public int StepNumber { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string? Agent { get; set; }
        public string? Tool { get; set; }
        public string? Type { get; set; }
        public string Action { get; set; } = string.Empty;
    }

    public class AgentToolResult
    {
        public string WorkflowId { get; set; } = string.Empty;
        public string Step { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object?> Data { get; set; } = new();
    }

    public class BudgetInsufficientException : Exception
    {
        public BudgetInsufficientException(string message) : base(message) { }
    }

    public class PaymentFailedException : Exception
    {
        public PaymentFailedException(string message) : base(message) { }
    }
}
