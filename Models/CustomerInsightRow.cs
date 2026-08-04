namespace HighFidelity.Ui.Models;

/// <summary>Correlated-subquery result shape — one row per order that's above its own country's average.</summary>
public class CustomerInsightRow
{
    public string Customer { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal CountryAveragePrice { get; set; }
}
