using System;
using System.ComponentModel.DataAnnotations;

namespace ReimbursementBudget.API.Data.Entities
{
    public class WorkflowStep
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WorkflowExecutionId { get; set; }

        public int StepNumber { get; set; }

        [MaxLength(100)]
        public string StepName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string AgentName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Status { get; set; } = WorkflowStepStatus.Pending;

        [MaxLength(50)]
        public string? StepType { get; set; } // AGENT | HUMAN_APPROVAL | TOOL

        public string? InputReference { get; set; }   // JSON snapshot of input
        public string? OutputReference { get; set; }  // JSON snapshot of output

        public string? ValidationResult { get; set; } // schema validation result

        public string? ErrorMessage { get; set; }

        public int RetryCount { get; set; } = 0;

        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation
        public WorkflowExecution WorkflowExecution { get; set; } = null!;
    }

    public static class WorkflowStepStatus
    {
        public const string Pending = "PENDING";
        public const string InProgress = "IN_PROGRESS";
        public const string Completed = "COMPLETED";
        public const string Failed = "FAILED";
        public const string Skipped = "SKIPPED";
        public const string WaitingForHuman = "WAITING_FOR_HUMAN";
    }
}
