namespace HighFidelity.Ui.Models;

/// <summary>
/// Shared row shape for every MongoDB-concepts card that returns orders
/// (joins, views, subqueries, union, search, pagination, top-N, FK+trigger).
/// CustomerSince is only present on the $lookup join result; null everywhere else.
/// </summary>
public class OrderRow
{
    public int Id { get; set; }
    public int Invoice { get; set; }
    public string Customer { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CustomerSince { get; set; }
}
