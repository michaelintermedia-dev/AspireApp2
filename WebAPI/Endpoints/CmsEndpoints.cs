using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WebAPI.Endpoints.CmsEndpoints;

public static class CmsEndpoints
{
    public static void MapCmsEndpoints(this WebApplication app)
    {
        var cms = app.MapGroup("/cms").WithTags("CMS");

        cms.MapGet("/content/{contentName}", async (
            string contentName,
            IDistributedCache cache,
            IHttpClientFactory httpClientFactory,
            ILogger<Program> logger) =>
        {

            string cacheKey = contentName;

            var cachedData = await cache.GetStringAsync(cacheKey);
            if (cachedData is not null)
            {
                return Results.Ok(JsonSerializer.Deserialize<JsonNode>(cachedData));
            }



            var client = httpClientFactory.CreateClient("umbraco");
            var requestUri = $"/umbraco/delivery/api/v2/content/item/{contentName}";

            logger.LogInformation("Calling Umbraco Delivery API: {BaseAddress}{RequestUri}", client.BaseAddress, requestUri);

            var response = await client.GetAsync(requestUri);

            logger.LogInformation("Umbraco response status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogWarning("Umbraco error response: {Body}", errorBody);
                return Results.StatusCode((int)response.StatusCode);
            }

            var json = await response.Content.ReadFromJsonAsync<JsonNode>();

            var propertiesOnly = json?["properties"];


            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(propertiesOnly), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

            return Results.Ok(propertiesOnly);
        });
    }
}
