using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReimbursementBudget.API.Data.Entities
{
    public class WorkflowExecution
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string WorkflowId { get; set; } = string.Empty; // human-readable e.g. WF-10001

        public Guid ExpenseClaimId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Objective { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Status { get; set; } = WorkflowStatus.Created;

        [MaxLength(50)]
        public string CurrentStep { get; set; } = string.Empty;

        public string? PlanJson { get; set; }          // full planned steps JSON
        public string? AgentOutputsJson { get; set; }  // accumulated agent outputs JSON

        public string? ApprovalStatus { get; set; }    // PENDING / APPROVED / REJECTED / REVISION_REQUIRED
        public string? ApproverId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalComment { get; set; }

        public string? PaymentStatus { get; set; }

        public string? FinalOutcome { get; set; }

        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public int MaxRetryCount { get; set; } = 3;
        public int MaxStepCount { get; set; } = 20;

        [MaxLength(100)]
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
    }

    public static class WorkflowStatus
    {
        public const string Created = "CREATED";
        public const string Planned = "PLANNED";
        public const string Extracting = "EXTRACTING";
        public const string PolicyChecking = "POLICY_CHECKING";
        public const string RiskChecking = "RISK_CHECKING";
        public const string WaitingForApproval = "WAITING_FOR_APPROVAL";
        public const string Approved = "APPROVED";
        public const string BudgetChecking = "BUDGET_CHECKING";
        public const string FinanceProcessing = "FINANCE_PROCESSING";
        public const string PaymentProcessing = "PAYMENT_PROCESSING";
        public const string Completed = "COMPLETED";
        public const string Failed = "FAILED";
        public const string Rejected = "REJECTED";
        public const string RevisionRequired = "REVISION_REQUIRED";
        public const string BudgetReviewRequired = "BUDGET_REVIEW_REQUIRED";
    }
}
