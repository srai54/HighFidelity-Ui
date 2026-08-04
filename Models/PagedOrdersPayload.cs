namespace HighFidelity.Ui.Models;

/// <summary>Deserialization envelope for GET /api/reports/orders-paged.</summary>
public class PagedOrdersPayload
{
    public List<OrderRow> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
    public int TotalPages { get; set; }
}
