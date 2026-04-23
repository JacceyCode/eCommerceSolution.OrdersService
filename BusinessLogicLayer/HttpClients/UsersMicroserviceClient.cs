using BusinessLogicLayer.DTO;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net.Http.Json;

namespace BusinessLogicLayer.HttpClients;

public class UsersMicroserviceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UsersMicroserviceClient> _logger;

    public UsersMicroserviceClient(HttpClient httpClient, ILogger<UsersMicroserviceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserDTO?> GetUserById(Guid userID)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"/api/users/{userID}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // User not found, return null or handle as needed
                    return null;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new HttpRequestException("Bad request", null, System.Net.HttpStatusCode.BadRequest);
                }
                else
                {
                    //throw new HttpRequestException($"Http request failed with status code: {response.StatusCode}", null, response.StatusCode);

                    // return fault data
                    return new UserDTO(
                        PersonName: "Temporarily Unavailable",
                        Email: "Temporarily Unavailable",
                        Gender: "Temporarily Unavailable",
                        UserID: Guid.Empty
                        );
                }
            }

            UserDTO? user = await response.Content.ReadFromJsonAsync<UserDTO>();

            if (user == null)
            {
                throw new ArgumentException("Invalid User ID");
            }

            return user;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Circuit breaker is open. Users microservice is temporarily unavailable, returning dummy data.");

            // return fault data
            return new UserDTO(
                PersonName: "Temporarily Unavailable (circuit breaker)",
                Email: "Temporarily Unavailable (circuit breaker)",
                Gender: "Temporarily Unavailable (circuit breaker)",
                UserID: Guid.Empty
                );
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogError(ex, "Timeout exception occurred while fetching user data, returning dummy data.");

            // return fault data
            return new UserDTO(
                PersonName: "Temporarily Unavailable (timeout)",
                Email: "Temporarily Unavailable (timeout)",
                Gender: "Temporarily Unavailable (timeout)",
                UserID: Guid.Empty
                );
        }
    }
}
