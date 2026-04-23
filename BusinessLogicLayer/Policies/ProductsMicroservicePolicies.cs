using BusinessLogicLayer.DTO;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Fallback;
using System.Net;
using System.Text;
using System.Text.Json;

namespace BusinessLogicLayer.Policies;

public class ProductsMicroservicePolicies : IProductsMicroservicePolicies
{
    private readonly ILogger<ProductsMicroservicePolicies> _logger;

    public ProductsMicroservicePolicies(ILogger<ProductsMicroservicePolicies> logger)
    {
        _logger = logger;
    }

    public IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy()
    {
        AsyncFallbackPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .FallbackAsync(async(context) => { 
                _logger.LogWarning("Fallback triggered. Returning default response.");

                ProductDTO product = new ProductDTO
                (
                    ProductID : Guid.Empty,
                    ProductName : "Temporarily Unavailable (fallback)",
                    Category : "Temporarily Unavailable (fallback)",
                    UnitPrice : 0,
                    QuantityInStock : 0
                );

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(product), Encoding.UTF8, "application/json")
                };
            });

        return policy;
    }
}
