using BusinessLogicLayer.DTO;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BusinessLogicLayer.HttpClients;

public class UsersMicroserviceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UsersMicroserviceClient> _logger;
    private readonly IDistributedCache _distributedCache;

    public UsersMicroserviceClient(HttpClient httpClient, ILogger<UsersMicroserviceClient> logger, IDistributedCache distributedCache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _distributedCache = distributedCache;
    }

    public async Task<UserDTO?> GetUserById(Guid userID)
    {
        try
        {
            // Check cache first
            string cacheKey = $"user:{userID}";
            string? cachedUser = await _distributedCache.GetStringAsync(cacheKey);

            if(cachedUser != null) {
                UserDTO? userFromCache = JsonSerializer.Deserialize<UserDTO>(cachedUser);

                return userFromCache;
            }

            HttpResponseMessage response = await _httpClient.GetAsync($"/api/users/{userID}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    UserDTO? fallbackUser = await response.Content.ReadFromJsonAsync<UserDTO>();

                    if (fallbackUser == null)
                    {
                        throw new NotImplementedException("Fallback policy was not implemented");
                    }

                    return fallbackUser;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    // User not found, return null or handle as needed
                    return null;
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new HttpRequestException("Bad request", null, HttpStatusCode.BadRequest);
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

            // Store user data in cache
            string cacheKeyToWrite = $"user:{userID}";
            string userJson = JsonSerializer.Serialize(user);
            DistributedCacheEntryOptions cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5)) // Cache for 5 minutes
                .SetSlidingExpiration(TimeSpan.FromMinutes(3));

            await _distributedCache.SetStringAsync(cacheKeyToWrite, userJson, cacheOptions);

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
