namespace MapsDemo.Api.DTOs;

public record GeocodeResult(Guid Id, string Address, decimal Latitude, decimal Longitude, DateTimeOffset QueriedAt);
