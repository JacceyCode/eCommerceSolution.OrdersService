using BusinessLogicLayer.DTO;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;
using System.Net;
using System.Text;
using System.Text.Json;

namespace BusinessLogicLayer.Policies;

public class PollyPolicies : IPollyPolicies
{
    private readonly ILogger<PollyPolicies> _logger;

    public PollyPolicies(ILogger<PollyPolicies> logger)
    {
        _logger = logger;
    }

    public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount)
    {
        AsyncRetryPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .WaitAndRetryAsync(
        retryCount: retryCount, // No. of Retry attempts
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(
            Math.Pow(2, retryAttempt)), // Exponential backoff strategy: 2s, 4s, 8s, 16s
        onRetry: (outcome, timespan, retryAttempt, context) =>
        {
            _logger.LogInformation($"Retrying... Attempt: {retryAttempt} after {timespan.TotalSeconds} seconds");
        });

        return policy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int allowedEvents, TimeSpan durationOfBreak)
    {
        AsyncCircuitBreakerPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: allowedEvents, // No. of failed attempts
        durationOfBreak: durationOfBreak, // Duration to break the circuit
        onBreak: (outcome, timespan) =>
        {
            _logger.LogInformation($"Circuit breaker opened for {timespan.TotalMinutes} minutes due to consecutive {allowedEvents} failures. The subsequent requests will be blocked.");
        },
        onReset: () =>
        {
            _logger.LogInformation("Circuit breaker closed. The subsequent requests will be allowed.");
        },
        onHalfOpen: () =>
        {
            _logger.LogInformation("Circuit breaker is half-open. The next request will test the circuit.");
        }
        );

        return policy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeout)
    {
        AsyncTimeoutPolicy<HttpResponseMessage> policy = Policy.TimeoutAsync<HttpResponseMessage>(timeout);

        return policy;
    }


    public IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy()
    {
        AsyncFallbackPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .FallbackAsync(async (context) => {
                _logger.LogWarning("Fallback triggered. Returning default response.");

                ProductDTO product = new ProductDTO
                (
                    ProductID: Guid.Empty,
                    ProductName: "Temporarily Unavailable (fallback)",
                    Category: "Temporarily Unavailable (fallback)",
                    UnitPrice: 0,
                    QuantityInStock: 0
                );

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(product), Encoding.UTF8, "application/json")
                };
            });

        return policy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetBulkheadIsolationPolicy(int maxParallelization, int maxQueuingActions)
    {
        AsyncBulkheadPolicy<HttpResponseMessage> policy = Policy.BulkheadAsync<HttpResponseMessage>(
            maxParallelization: maxParallelization, // Max concurrent executions
            maxQueuingActions: maxQueuingActions, // Max queued actions when bulkhead is full
            onBulkheadRejectedAsync: context =>
            {
                _logger.LogWarning("Bulkhead limit reached. Request rejected.");

                throw new BulkheadRejectedException("Bulkhead queue is full");
            }
            );

        return policy;
    }
}
