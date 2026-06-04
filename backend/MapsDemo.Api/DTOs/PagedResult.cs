namespace MapsDemo.Api.DTOs;

public record PagedResult<T>(IEnumerable<T> Data, int Page, int PageSize, int TotalCount, int TotalPages);
