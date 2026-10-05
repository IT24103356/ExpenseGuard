using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReimbursementBudget.API.DTOs;
using ReimbursementBudget.API.Services;

namespace ReimbursementBudget.API.Controllers
{
    [ApiController]
    [Route("api/budgets")]
    [Authorize]
    public class BudgetsController : ControllerBase
    {
        private readonly IBudgetService _service;

        public BudgetsController(IBudgetService service) => _service = service;

        /// <summary>GET /api/budgets</summary>
        [HttpGet]
        [Authorize(Roles = "Finance,Admin,Manager")]
        public async Task<IActionResult> GetAllBudgets([FromQuery] int? fiscalYear)
        {
            var result = await _service.GetAllBudgetsAsync(fiscalYear);
            return Ok(result.Data);
        }

        /// <summary>GET /api/budgets/{id}</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Finance,Admin,Manager")]
        public async Task<IActionResult> GetBudget(Guid id)
        {
            var result = await _service.GetBudgetAsync(id);
            if (!result.IsSuccess) return NotFound(new { error = result.ErrorMessage });
            return Ok(result.Data);
        }

        /// <summary>POST /api/budgets — Admin creates budget</summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBudget([FromBody] CreateBudgetRequest request)
        {
            if (request.AllocatedAmount <= 0)
                return BadRequest(new { error = "AllocatedAmount must be positive" });

            var result = await _service.CreateBudgetAsync(request);
            if (!result.IsSuccess) return BadRequest(new { error = result.ErrorMessage });
            return CreatedAtAction(nameof(GetBudget), new { id = result.Data.Id }, result.Data);
        }

        /// <summary>PUT /api/budgets/{id} — Admin updates/reallocates budget</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBudget(Guid id, [FromBody] UpdateBudgetRequest request)
        {
            if (request.AllocatedAmount <= 0)
                return BadRequest(new { error = "AllocatedAmount must be positive" });

            var result = await _service.UpdateBudgetAsync(id, request);
            if (!result.IsSuccess) return BadRequest(new { error = result.ErrorMessage });
            return Ok(result.Data);
        }

        /// <summary>GET /api/budgets/{id}/transactions</summary>
        [HttpGet("{id:guid}/transactions")]
        [Authorize(Roles = "Finance,Admin")]
        public async Task<IActionResult> GetBudgetTransactions(Guid id)
        {
            var result = await _service.GetBudgetTransactionsAsync(id);
            if (!result.IsSuccess) return BadRequest(new { error = result.ErrorMessage });
            return Ok(result.Data);
        }

        /// <summary>GET /api/budgets/{id}/summary — Budget utilization summary</summary>
        [HttpGet("{id:guid}/summary")]
        [Authorize(Roles = "Finance,Admin,Manager")]
        public async Task<IActionResult> GetBudgetSummary(Guid id)
        {
            var result = await _service.GetBudgetSummaryAsync(id);
            if (!result.IsSuccess) return NotFound(new { error = result.ErrorMessage });
            return Ok(result.Data);
        }

        /// <summary>GET /api/budgets/department/{departmentId}</summary>
        [HttpGet("department/{departmentId}")]
        [Authorize(Roles = "Finance,Admin,Manager")]
        public async Task<IActionResult> GetDepartmentBudgets(string departmentId)
        {
            var result = await _service.GetDepartmentBudgetsAsync(departmentId);
            return Ok(result.Data);
        }
    }
}
