namespace MapsDemo.Api.Data;

public class GeocodeQuery
{
    public Guid Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string RequestPayload { get; set; } = string.Empty;
    public string ResponsePayload { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTimeOffset QueriedAt { get; set; }
}
