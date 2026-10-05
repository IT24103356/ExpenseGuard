using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReimbursementBudget.API.DTOs;
using ReimbursementBudget.API.Services;

namespace ReimbursementBudget.API.Controllers
{
    [ApiController]
    [Route("api/reimbursements")]
    [Authorize]
    public class ReimbursementsController : ControllerBase
    {
        private readonly IReimbursementService _service;

        public ReimbursementsController(IReimbursementService service) => _service = service;

        /// <summary>GET /api/reimbursements/finance-queue — Finance role only</summary>
        [HttpGet("finance-queue")]
        [Authorize(Roles = "Finance,Admin")]
        public async Task<IActionResult> GetFinanceQueue([FromQuery] FinanceQueueFilter filter)
        {
            var result = await _service.GetFinanceQueueAsync(filter);
            if (!result.IsSuccess) return BadRequest(new { error = result.ErrorMessage });
            return Ok(result.Data);
        }

        /// <summary>GET /api/reimbursements/{id}</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Finance,Admin,Manager,Employee")]
        public async Task<IActionResult> GetReimbursement(Guid id)
        {
            var result = await _service.GetReimbursementAsync(id);
            if (!result.IsSuccess) return NotFound(new { error = result.ErrorMessage });
            return Ok(result.Data);
        }

        /// <summary>GET /api/reimbursements/by-claim/{claimId}</summary>
        [HttpGet("by-claim/{claimId:guid}")]
        [Authorize(Roles = "Finance,Admin,Manager,Employee")]
        public async Task<IActionResult> GetByClaimId(Guid claimId)
        {
            var result = await _service.GetByClaimIdAsync(claimId);
            if (!result.IsSuccess) return NotFound(new { error = result.ErrorMessage });
            return Ok(result.Data);
        }

        /// <summary>GET /api/reimbursements/employee/{employeeId} — Employee sees own reimbursements</summary>
        [HttpGet("employee/{employeeId}")]
        [Authorize(Roles = "Employee,Finance,Admin")]
        public async Task<IActionResult> GetEmployeeReimbursements(string employeeId)
        {
            var result = await _service.GetEmployeeReimbursementsAsync(employeeId);
            if (!result.IsSuccess) return BadRequest(new { error = result.ErrorMessage });
            return Ok(result.Data);
        }

        /// <summary>POST /api/reimbursements/{id}/process — Finance processes a reimbursement</summary>
        [HttpPost("{id:guid}/process")]
        [Authorize(Roles = "Finance,Admin")]
        public async Task<IActionResult> ProcessReimbursement(Guid id)
        {
            var userId = User.FindFirst("sub")?.Value ?? "UNKNOWN";
            var result = await _service.ProcessReimbursementAsync(id, userId);
            if (!result.IsSuccess) return BadRequest(new { error = result.ErrorMessage });
            return Ok(result.Data);
        }

        /// <summary>POST /api/reimbursements/{id}/payment — Finance triggers payment</summary>
        [HttpPost("{id:guid}/payment")]
        [Authorize(Roles = "Finance,Admin")]
        public async Task<IActionResult> SubmitPayment(Guid id)
        {
            var userId = User.FindFirst("sub")?.Value ?? "UNKNOWN";
            var result = await _service.SubmitPaymentAsync(id, userId);
            if (!result.IsSuccess) return BadRequest(new { error = result.ErrorMessage });
            return Ok(result.Data);
        }
    }
}
