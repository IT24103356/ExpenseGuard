using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ReimbursementBudget.API.Infrastructure.Payment
{
    /// <summary>
    /// Mock/sandbox payment provider. Does NOT process real money.
    /// Simulates SUCCESS, FAILED, and TIMEOUT scenarios deterministically
    /// based on amount ranges (configurable) so tests are repeatable.
    /// </summary>
    public class SandboxPaymentProvider : IPaymentProvider
    {
        private readonly ILogger<SandboxPaymentProvider> _logger;
        private readonly decimal _failThreshold;
        private readonly decimal _timeoutThreshold;
        private static readonly Dictionary<string, (string status, string txnId)> _store = new();
        private static long _counter = 10000;

        public SandboxPaymentProvider(
            ILogger<SandboxPaymentProvider> logger,
            IConfiguration config)
        {
            _logger = logger;
            // Read from environment / appsettings — never hard-coded secrets
            _failThreshold = config.GetValue<decimal>("PaymentSandbox:FailThreshold", 9_000_000);
            _timeoutThreshold = config.GetValue<decimal>("PaymentSandbox:TimeoutThreshold", 9_500_000);
        }

        public Task<PaymentResponse> CreatePaymentAsync(PaymentRequest request)
        {
            _logger.LogInformation("[SANDBOX] CreatePayment IdempotencyKey={Key} Amount={Amount} {Currency}",
                request.IdempotencyKey, request.Amount, request.Currency);

            // Idempotency: if already processed, return same result
            if (_store.TryGetValue(request.IdempotencyKey, out var existing))
            {
                _logger.LogWarning("[SANDBOX] Duplicate payment detected for key {Key}", request.IdempotencyKey);
                return Task.FromResult(new PaymentResponse(
                    true, existing.txnId, existing.status, request.Amount, request.Currency, null,
                    $"{{\"duplicate\":true,\"transactionId\":\"{existing.txnId}\"}}"));
            }

            var txnId = $"PAY-{System.Threading.Interlocked.Increment(ref _counter)}";

            if (request.Amount >= _timeoutThreshold)
            {
                _store[request.IdempotencyKey] = ("TIMEOUT", txnId);
                _logger.LogWarning("[SANDBOX] Payment TIMEOUT for {TxnId}", txnId);
                return Task.FromResult(new PaymentResponse(
                    false, txnId, "TIMEOUT", request.Amount, request.Currency,
                    "Payment gateway timed out",
                    $"{{\"status\":\"TIMEOUT\",\"transactionId\":\"{txnId}\"}}"));
            }

            if (request.Amount >= _failThreshold)
            {
                _store[request.IdempotencyKey] = ("FAILED", txnId);
                _logger.LogWarning("[SANDBOX] Payment FAILED for {TxnId}", txnId);
                return Task.FromResult(new PaymentResponse(
                    false, txnId, "FAILED", request.Amount, request.Currency,
                    "Insufficient funds in sandbox account",
                    $"{{\"status\":\"FAILED\",\"transactionId\":\"{txnId}\"}}"));
            }

            _store[request.IdempotencyKey] = ("COMPLETED", txnId);
            _logger.LogInformation("[SANDBOX] Payment COMPLETED {TxnId}", txnId);
            return Task.FromResult(new PaymentResponse(
                true, txnId, "COMPLETED", request.Amount, request.Currency, null,
                $"{{\"success\":true,\"transactionId\":\"{txnId}\",\"status\":\"COMPLETED\",\"amount\":{request.Amount},\"currency\":\"{request.Currency}\"}}"));
        }

        public Task<PaymentStatusResponse> GetPaymentStatusAsync(string transactionId)
        {
            foreach (var kv in _store)
            {
                if (kv.Value.txnId == transactionId)
                {
                    return Task.FromResult(new PaymentStatusResponse(
                        true, transactionId, kv.Value.status, 0, "LKR", null));
                }
            }
            return Task.FromResult(new PaymentStatusResponse(
                false, transactionId, "NOT_FOUND", 0, "LKR", "Transaction not found in sandbox"));
        }

        public Task<PaymentResponse> CancelPaymentAsync(string transactionId)
        {
            foreach (var kv in _store)
            {
                if (kv.Value.txnId == transactionId)
                {
                    _store[kv.Key] = ("CANCELLED", transactionId);
                    _logger.LogInformation("[SANDBOX] Payment CANCELLED {TxnId}", transactionId);
                    return Task.FromResult(new PaymentResponse(
                        true, transactionId, "CANCELLED", 0, "LKR", null,
                        $"{{\"status\":\"CANCELLED\",\"transactionId\":\"{transactionId}\"}}"));
                }
            }
            return Task.FromResult(new PaymentResponse(
                false, transactionId, "NOT_FOUND", 0, "LKR", "Transaction not found", null));
        }
    }
}
