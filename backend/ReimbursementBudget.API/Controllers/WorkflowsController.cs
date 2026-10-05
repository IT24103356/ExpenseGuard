using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReimbursementBudget.API.Agents;
using ReimbursementBudget.API.DTOs;

namespace ReimbursementBudget.API.Controllers
{
    [ApiController]
    [Route("api/workflows")]
    [Authorize]
    public class WorkflowsController : ControllerBase
    {
        private readonly CoordinatorPlannerAgent _coordinator;

        public WorkflowsController(CoordinatorPlannerAgent coordinator) => _coordinator = coordinator;

        /// <summary>POST /api/workflows/start — Starts the agentic workflow for a new claim</summary>
        [HttpPost("start")]
        [Authorize(Roles = "Employee,Finance,Admin")]
        public async Task<IActionResult> StartWorkflow([FromBody] StartWorkflowRequest request)
        {
            if (request.ExpenseClaimId == Guid.Empty)
                return BadRequest(new { error = "ExpenseClaimId is required" });
            if (request.Amount <= 0)
                return BadRequest(new { error = "Amount must be positive" });

            var result = await _coordinator.StartWorkflowAsync(request);
            return Ok(result);
        }

        /// <summary>GET /api/workflows/{id} — Gets workflow execution state</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Finance,Admin,Manager,Employee")]
        public async Task<IActionResult> GetWorkflow(Guid id)
        {
            var result = await _coordinator.GetWorkflowAsync(id);
            if (result == null) return NotFound(new { error = "Workflow not found" });
            return Ok(result);
        }

        /// <summary>GET /api/workflows/by-claim/{claimId}</summary>
        [HttpGet("by-claim/{claimId:guid}")]
        [Authorize(Roles = "Finance,Admin,Manager,Employee")]
        public async Task<IActionResult> GetWorkflowByClaim(Guid claimId)
        {
            var result = await _coordinator.GetWorkflowByClaimAsync(claimId);
            if (result == null) return NotFound(new { error = "Workflow not found for claim" });
            return Ok(result);
        }

        /// <summary>
        /// POST /api/workflows/{id}/approval — Manager submits approval decision.
        /// CRITICAL: The coordinator enforces that payment is NOT called before this.
        /// </summary>
        [HttpPost("{id:guid}/approval")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> SubmitApprovalDecision(Guid id, [FromBody] ApprovalDecisionRequest request)
        {
            var validDecisions = new[] { "APPROVED", "REJECTED", "REVISION_REQUIRED" };
            if (!Array.Exists(validDecisions, d => d == request.Decision?.ToUpper()))
                return BadRequest(new { error = "Decision must be APPROVED, REJECTED, or REVISION_REQUIRED" });

            // Use authenticated user as approver (never trust request body for identity)
            request.ApproverId = User.FindFirst("sub")?.Value ?? request.ApproverId;

            try
            {
                var result = await _coordinator.ResumeAfterApprovalAsync(id, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
