namespace MapsDemo.Api.DTOs;

public record GeocodeRequest(string PlaceId, string Address, string SessionToken);
