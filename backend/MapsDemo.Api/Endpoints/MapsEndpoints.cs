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
        MapsApiSettings mapsSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
            return Results.Problem(title: "Input is required", statusCode: 400);

        if (request.Input.Trim().Length < 3)
            return Results.Problem(title: "Input must be at least 3 characters", statusCode: 400);

        if (string.IsNullOrWhiteSpace(request.SessionToken) || !Guid.TryParse(request.SessionToken, out _))
            return Results.Problem(title: "SessionToken must be a valid UUID", statusCode: 400);

        logger.LogInformation("Autocomplete request for input: {Input}", request.Input);

        try
        {
            var client = httpClientFactory.CreateClient("GoogleMaps");
            var payload = JsonSerializer.Serialize(
                new { input = request.Input, sessionToken = request.SessionToken },
                JsonOptions);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                "https://places.googleapis.com/v1/places:autocomplete");
            httpRequest.Headers.Add("X-Goog-Api-Key", mapsSettings.ApiKey);
            httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(httpRequest, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

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

            logger.LogInformation("Autocomplete returned {Count} suggestions for: {Input}", suggestions.Count, request.Input);
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
        MapsApiSettings mapsSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PlaceId))
            return Results.Problem(title: "PlaceId is required", statusCode: 400);

        if (request.PlaceId.Length > 500)
            return Results.Problem(title: "PlaceId exceeds maximum allowed length", statusCode: 400);

        if (string.IsNullOrWhiteSpace(request.SessionToken) || !Guid.TryParse(request.SessionToken, out _))
            return Results.Problem(title: "SessionToken must be a valid UUID", statusCode: 400);

        logger.LogInformation("Geocode request for placeId: {PlaceId}", request.PlaceId);

        try
        {
            var client = httpClientFactory.CreateClient("GoogleMaps");
            var url = $"https://places.googleapis.com/v1/places/{Uri.EscapeDataString(request.PlaceId)}";

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequest.Headers.Add("X-Goog-Api-Key", mapsSettings.ApiKey);
            httpRequest.Headers.Add("X-Goog-FieldMask", "location");
            var response = await client.SendAsync(httpRequest, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Places Details API error {Status}: {Body}", response.StatusCode, body);
                return Results.Problem("Error calling Places Details API", statusCode: 502);
            }

            var doc = JsonDocument.Parse(body);

            if (!doc.RootElement.TryGetProperty("location", out var location))
            {
                logger.LogWarning("Places Details API returned no location for placeId: {PlaceId}", request.PlaceId);
                return Results.Problem(title: "No location found for the given place", statusCode: 404);
            }

            var lat = location.GetProperty("latitude").GetDecimal();
            var lng = location.GetProperty("longitude").GetDecimal();

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
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Geocode successful for address: {Address}, lat: {Lat}, lng: {Lng}",
                request.Address, lat, lng);
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
        ILogger<Program> logger,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 10,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null)
    {
        if (page < 1)
            return Results.Problem(title: "Page must be >= 1", statusCode: 400);

        if (pageSize < 1 || pageSize > 100)
            return Results.Problem(title: "pageSize must be between 1 and 100", statusCode: 400);

        logger.LogInformation("History request: page={Page}, pageSize={PageSize}, dateFrom={DateFrom}, dateTo={DateTo}",
            page, pageSize, dateFrom, dateTo);

        try
        {
            var query = db.GeocodeQueries.AsQueryable();

            if (dateFrom.HasValue)
                query = query.Where(q => q.QueriedAt >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(q => q.QueriedAt < dateTo.Value.AddDays(1));

            var totalCount = await query.CountAsync(cancellationToken);
            var data = await query
                .OrderByDescending(q => q.QueriedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(q => new GeocodeResult(q.Id, q.Address, q.Latitude, q.Longitude, q.QueriedAt))
                .ToListAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            logger.LogInformation("History returned {Count} of {Total} records", data.Count, totalCount);
            return Results.Ok(new PagedResult<GeocodeResult>(data, page, pageSize, totalCount, totalPages));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "History query failed");
            return Results.Problem("Internal error retrieving history", statusCode: 500);
        }
    }
}
