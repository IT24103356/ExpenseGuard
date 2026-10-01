using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReimbursementBudget.API.Services;

namespace ReimbursementBudget.API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "Finance,Admin,Manager")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportingService _service;

        public ReportsController(IReportingService service) => _service = service;

        /// <summary>GET /api/reports/spend-vs-budget</summary>
        [HttpGet("spend-vs-budget")]
        public async Task<IActionResult> GetSpendVsBudget(
            [FromQuery] int? fiscalYear,
            [FromQuery] string? departmentId)
        {
            var result = await _service.GetSpendVsBudgetReportAsync(fiscalYear, departmentId);
            return Ok(result);
        }

        /// <summary>GET /api/reports/departments/{id}/spending</summary>
        [HttpGet("departments/{departmentId}/spending")]
        public async Task<IActionResult> GetDepartmentSpending(
            string departmentId,
            [FromQuery] int? fiscalYear)
        {
            var result = await _service.GetDepartmentSpendingAsync(departmentId, fiscalYear);
            return Ok(result);
        }

        /// <summary>GET /api/reports/monthly-spending</summary>
        [HttpGet("monthly-spending")]
        public async Task<IActionResult> GetMonthlySpending(
            [FromQuery] int? fiscalYear,
            [FromQuery] string? departmentId)
        {
            var result = await _service.GetMonthlySpendingAsync(fiscalYear, departmentId);
            return Ok(result);
        }

        /// <summary>GET /api/reports/category-spending</summary>
        [HttpGet("category-spending")]
        public async Task<IActionResult> GetCategorySpending(
            [FromQuery] int? fiscalYear,
            [FromQuery] string? departmentId)
        {
            var result = await _service.GetCategorySpendingAsync(fiscalYear, departmentId);
            return Ok(result);
        }

        /// <summary>GET /api/reports/reimbursement-summary</summary>
        [HttpGet("reimbursement-summary")]
        public async Task<IActionResult> GetReimbursementSummary(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var result = await _service.GetReimbursementSummaryAsync(from, to);
            return Ok(result);
        }

        /// <summary>GET /api/reports/payment-summary</summary>
        [HttpGet("payment-summary")]
        public async Task<IActionResult> GetPaymentSummary(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var result = await _service.GetPaymentSummaryAsync(from, to);
            return Ok(result);
        }

        /// <summary>GET /api/reports/finance-dashboard</summary>
        [HttpGet("finance-dashboard")]
        public async Task<IActionResult> GetFinanceDashboard([FromQuery] int? fiscalYear)
        {
            var result = await _service.GetFinanceDashboardAsync(fiscalYear);
            return Ok(result);
        }
    }
}
