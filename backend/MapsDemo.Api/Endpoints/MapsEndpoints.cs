using MapsDemo.Api.Data;
using MapsDemo.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace MapsDemo.Api.Endpoints;

public static class MapsEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static void MapMapsEndpoints(this WebApplication app)
    {
        app.MapPost("/api/maps/autocomplete", HandleAutocomplete);
        app.MapPost("/api/maps/geocode", HandleGeocode);
        app.MapGet("/api/maps/history", HandleHistory);
    }

    private static async Task<IResult> HandleAutocomplete(
        AutocompleteRequest request,
        IHttpClientFactory httpClientFactory,
        ILogger<Program> logger)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
            return Results.Problem(title: "Input is required", statusCode: 400);

        if (request.Input.Trim().Length < 3)
            return Results.Problem(title: "Input must be at least 3 characters", statusCode: 400);

        if (string.IsNullOrWhiteSpace(request.SessionToken) || !Guid.TryParse(request.SessionToken, out _))
            return Results.Problem(title: "SessionToken must be a valid UUID", statusCode: 400);

        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            return Results.Problem("Google Maps API key not configured", statusCode: 500);

        try
        {
            var client = httpClientFactory.CreateClient("GoogleMaps");
            var payload = JsonSerializer.Serialize(
                new { input = request.Input, sessionToken = request.SessionToken },
                JsonOptions);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                "https://places.googleapis.com/v1/places:autocomplete");
            httpRequest.Headers.Add("X-Goog-Api-Key", apiKey);
            httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(httpRequest);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Places API error {Status}: {Body}", response.StatusCode, body);
                return Results.Problem("Error calling Places API", statusCode: 502);
            }

            var suggestions = new List<AutocompleteSuggestion>();
            var doc = JsonDocument.Parse(body);

            if (doc.RootElement.TryGetProperty("suggestions", out var suggestionsEl))
            {
                foreach (var item in suggestionsEl.EnumerateArray())
                {
                    if (!item.TryGetProperty("placePrediction", out var prediction)) continue;
                    var placeId = prediction.GetProperty("placeId").GetString() ?? "";
                    var text = prediction.GetProperty("text").GetProperty("text").GetString() ?? "";
                    suggestions.Add(new AutocompleteSuggestion(placeId, text));
                }
            }

            return Results.Ok(suggestions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Autocomplete failed");
            return Results.Problem("Internal error during autocomplete", statusCode: 500);
        }
    }

    private static async Task<IResult> HandleGeocode(
        GeocodeRequest request,
        AppDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<Program> logger)
    {
        if (string.IsNullOrWhiteSpace(request.PlaceId))
            return Results.Problem(title: "PlaceId is required", statusCode: 400);

        if (string.IsNullOrWhiteSpace(request.SessionToken) || !Guid.TryParse(request.SessionToken, out _))
            return Results.Problem(title: "SessionToken must be a valid UUID", statusCode: 400);

        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            return Results.Problem("Google Maps API key not configured", statusCode: 500);

        try
        {
            var client = httpClientFactory.CreateClient("GoogleMaps");
            var url = $"https://maps.googleapis.com/maps/api/geocode/json" +
                      $"?place_id={Uri.EscapeDataString(request.PlaceId)}";

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequest.Headers.Add("X-Goog-Api-Key", apiKey);
            var response = await client.SendAsync(httpRequest);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Geocoding API error {Status}: {Body}", response.StatusCode, body);
                return Results.Problem("Error calling Geocoding API", statusCode: 502);
            }

            var doc = JsonDocument.Parse(body);
            var status = doc.RootElement.GetProperty("status").GetString();

            if (status != "OK")
            {
                logger.LogError("Geocoding API returned status: {Status}", status);
                return Results.Problem($"Geocoding API returned: {status}", statusCode: 502);
            }

            var results = doc.RootElement.GetProperty("results");
            if (results.GetArrayLength() == 0)
            {
                logger.LogWarning("Geocoding API returned OK but empty results for placeId: {PlaceId}", request.PlaceId);
                return Results.Problem(title: "No results found for the given place", statusCode: 404);
            }

            var location = results[0]
                .GetProperty("geometry")
                .GetProperty("location");

            var lat = location.GetProperty("lat").GetDecimal();
            var lng = location.GetProperty("lng").GetDecimal();

            var requestPayload = JsonSerializer.Serialize(
                new { placeId = request.PlaceId, address = request.Address },
                JsonOptions);

            var query = new GeocodeQuery
            {
                Id = Guid.NewGuid(),
                Address = request.Address,
                RequestPayload = requestPayload,
                ResponsePayload = body,
                Latitude = lat,
                Longitude = lng,
                QueriedAt = DateTimeOffset.UtcNow
            };

            db.GeocodeQueries.Add(query);
            await db.SaveChangesAsync();

            return Results.Ok(new GeocodeResult(query.Id, query.Address, query.Latitude, query.Longitude, query.QueriedAt));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Geocode failed");
            return Results.Problem("Internal error during geocode", statusCode: 500);
        }
    }

    private static async Task<IResult> HandleHistory(
        AppDbContext db,
        int page = 1,
        int pageSize = 10,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null)
    {
        var query = db.GeocodeQueries.AsQueryable();

        if (dateFrom.HasValue)
            query = query.Where(q => q.QueriedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(q => q.QueriedAt < dateTo.Value.AddDays(1));

        var totalCount = await query.CountAsync();
        var data = await query
            .OrderByDescending(q => q.QueriedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new GeocodeResult(q.Id, q.Address, q.Latitude, q.Longitude, q.QueriedAt))
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Results.Ok(new PagedResult<GeocodeResult>(data, page, pageSize, totalCount, totalPages));
    }
}
