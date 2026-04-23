using BusinessLogicLayer.DTO;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly.Bulkhead;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BusinessLogicLayer.HttpClients;

public class ProductsMicroserviceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductsMicroserviceClient> _logger;
    private readonly IDistributedCache _distributedCache;

    public ProductsMicroserviceClient(HttpClient httpClient, ILogger<ProductsMicroserviceClient> logger, IDistributedCache distributedCache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _distributedCache = distributedCache;
    }

    public async Task<ProductDTO?> GetProductById(Guid productID)
    {
        try
        {
            // Check redis before calling service
            string cacheKey = $"product:{productID}";
            string? cachedProduct = await _distributedCache.GetStringAsync(cacheKey);

            if(cachedProduct != null) {
                ProductDTO? productFromCache = JsonSerializer.Deserialize<ProductDTO>(cachedProduct);

                return productFromCache;
            }

            // Call the service if not found in cache
            HttpResponseMessage response = await _httpClient.GetAsync($"/api/products/search/product-id/{productID}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    ProductDTO? productFromFallbackPolicy = await response.Content.ReadFromJsonAsync<ProductDTO>();

                    if (productFromFallbackPolicy == null)
                    {
                        throw new NotImplementedException("Fallback policy was not implemented");
                    }

                    return productFromFallbackPolicy;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    // Product not found, return null or handle as needed
                    return null;
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new HttpRequestException("Bad request", null, HttpStatusCode.BadRequest);
                }
                else
                {
                    throw new HttpRequestException($"Http request failed with status code: {response.StatusCode}", null, response.StatusCode);
                }
            }

            ProductDTO? product = await response.Content.ReadFromJsonAsync<ProductDTO>();

            if (product == null)
            {
                throw new ArgumentException("Invalid Product ID");
            }

            // Cache the product in Redis for future requests
            string cacheKeyToWrite = $"product:{product.ProductID}";
            string serializedProduct = JsonSerializer.Serialize(product);

            // Set cache expiration to 30seconds (optional)
            DistributedCacheEntryOptions cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(300))
                .SetSlidingExpiration(TimeSpan.FromSeconds(100));

            await _distributedCache.SetStringAsync(cacheKeyToWrite, serializedProduct, cacheOptions);

            // Return the product
            return product;
        }
        catch (BulkheadRejectedException ex)
        {
            _logger.LogError(ex, "Bulkhead isolation blocks the request since the request queue is full.");

            return new ProductDTO
                (
                    ProductID: Guid.Empty,
                    ProductName: "Temporarily Unavailable (Bulkhead)",
                    Category: "Temporarily Unavailable (Bulkhead)",
                    UnitPrice: 0,
                    QuantityInStock: 0
                );
        }
    }
}
