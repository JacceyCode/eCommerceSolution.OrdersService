using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace BusinessLogicLayer.Policies;

public class UsersMicroservicePolicies : IUsersMicroservicePolicies
{
    private readonly ILogger<UsersMicroservicePolicies> _logger;
    private readonly IPollyPolicies _pollyPolicies;

    public UsersMicroservicePolicies(ILogger<UsersMicroservicePolicies> logger, IPollyPolicies pollyPolicies)
    {
        _logger = logger;
        _pollyPolicies = pollyPolicies;
    }

    //public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    //{
    //    AsyncRetryPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    //    .WaitAndRetryAsync(
    //    retryCount: 5, // No. of Retry attempts
    //    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(
    //        Math.Pow(2, retryAttempt)), // Exponential backoff strategy: 2s, 4s, 8s, 16s
    //    onRetry: (outcome, timespan, retryAttempt, context) =>
    //    {
    //        _logger.LogInformation($"Retrying... Attempt: {retryAttempt} after {timespan.TotalSeconds} seconds");
    //    });

    //    return policy;
    //}

    //public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    //{
    //    AsyncCircuitBreakerPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    //    .CircuitBreakerAsync(
    //    handledEventsAllowedBeforeBreaking: 3, // No. of failed attempts
    //    durationOfBreak: TimeSpan.FromMinutes(2), // Duration to break the circuit
    //    onBreak: (outcome, timespan) =>
    //    {
    //        _logger.LogInformation($"Circuit breaker opened for {timespan.TotalMinutes} minutes due to consecutive 3 failures. The subsequent requests will be blocked.");
    //    },
    //    onReset: () =>
    //    {
    //        _logger.LogInformation("Circuit breaker closed. The subsequent requests will be allowed.");
    //    },
    //    onHalfOpen: () =>
    //    {
    //        _logger.LogInformation("Circuit breaker is half-open. The next request will test the circuit.");
    //    }
    //    );

    //    return policy;
    //}

    //public IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    //{
    //    AsyncTimeoutPolicy<HttpResponseMessage> policy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(1500));

    //    return policy;
    //}

    public IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy()
    {
        var retryPolicy = _pollyPolicies.GetRetryPolicy(5);
        var circuitBreakerPolicy = _pollyPolicies.GetCircuitBreakerPolicy(3, TimeSpan.FromMinutes(2));
        var timeoutPolicy = _pollyPolicies.GetTimeoutPolicy(TimeSpan.FromSeconds(5));

        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
    }
}
