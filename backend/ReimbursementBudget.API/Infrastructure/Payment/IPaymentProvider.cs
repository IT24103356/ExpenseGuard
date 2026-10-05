using System.Threading.Tasks;

namespace ReimbursementBudget.API.Infrastructure.Payment
{
    public interface IPaymentProvider
    {
        Task<PaymentResponse> CreatePaymentAsync(PaymentRequest request);
        Task<PaymentStatusResponse> GetPaymentStatusAsync(string transactionId);
        Task<PaymentResponse> CancelPaymentAsync(string transactionId);
    }

    public record PaymentRequest(
        string IdempotencyKey,
        string ReimbursementId,
        string EmployeeId,
        string DepartmentId,
        decimal Amount,
        string Currency,
        string Description
    );

    public record PaymentResponse(
        bool Success,
        string? TransactionId,
        string Status,
        decimal Amount,
        string Currency,
        string? FailureReason,
        string? RawResponse
    );

    public record PaymentStatusResponse(
        bool Success,
        string TransactionId,
        string Status,
        decimal Amount,
        string Currency,
        string? FailureReason
    );
}
