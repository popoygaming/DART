namespace DART.API.Net.DTOs;

public class PagedRecordsResponse
{
    public IReadOnlyList<RecordResponse> Items { get; set; } = Array.Empty<RecordResponse>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }
}
