using BusinessLogicLayer.DTO;
using System.Net.Http.Json;

namespace BusinessLogicLayer.HttpClients;

public class ProductsMicroserviceClient
{
    private readonly HttpClient _httpClient;

    public ProductsMicroserviceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductDTO?> GetProductById(Guid productID)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"api/products/search/product-id/{productID}");

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Product not found, return null or handle as needed
                return null;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                throw new HttpRequestException("Bad request", null, System.Net.HttpStatusCode.BadRequest);
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

        return product;
    }
}
