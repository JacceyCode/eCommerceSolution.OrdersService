using BusinessLogicLayer.DTO;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Bulkhead;
using Polly.Fallback;
using System.Net;
using System.Text;
using System.Text.Json;

namespace BusinessLogicLayer.Policies;

public class ProductsMicroservicePolicies : IProductsMicroservicePolicies
{
    private readonly ILogger<ProductsMicroservicePolicies> _logger;
    private readonly IPollyPolicies _pollyPolicies;

    public ProductsMicroservicePolicies(ILogger<ProductsMicroservicePolicies> logger, IPollyPolicies pollyPolicies)
    {
        _logger = logger;
        _pollyPolicies = pollyPolicies;
    }

    //public IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy()
    //{
    //    AsyncFallbackPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    //        .FallbackAsync(async(context) => { 
    //            _logger.LogWarning("Fallback triggered. Returning default response.");

    //            ProductDTO product = new ProductDTO
    //            (
    //                ProductID : Guid.Empty,
    //                ProductName : "Temporarily Unavailable (fallback)",
    //                Category : "Temporarily Unavailable (fallback)",
    //                UnitPrice : 0,
    //                QuantityInStock : 0
    //            );

    //            return new HttpResponseMessage(HttpStatusCode.OK)
    //            {
    //                Content = new StringContent(JsonSerializer.Serialize(product), Encoding.UTF8, "application/json")
    //            };
    //        });

    //    return policy;
    //}

    //public IAsyncPolicy<HttpResponseMessage> GetBulkheadIsolationPolicy()
    //{
    //    AsyncBulkheadPolicy<HttpResponseMessage> policy = Policy.BulkheadAsync<HttpResponseMessage>(
    //        maxParallelization: 2, // Max concurrent executions
    //        maxQueuingActions: 40, // Max queued actions when bulkhead is full
    //        onBulkheadRejectedAsync: context =>
    //        {
    //            _logger.LogWarning("Bulkhead limit reached. Request rejected.");

    //            throw new BulkheadRejectedException("Bulkhead queue is full");
    //        }
    //        );

    //    return policy;
    //}

    public IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy()
    {
        var fallbackPolicy = _pollyPolicies.GetFallbackPolicy();
        var bulkheadPolicy = _pollyPolicies.GetBulkheadIsolationPolicy(2, 40);

        return Policy.WrapAsync(fallbackPolicy, bulkheadPolicy);
    }
}
