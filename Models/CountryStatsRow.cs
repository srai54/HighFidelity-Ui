namespace HighFidelity.Ui.Models;

/// <summary>Aggregate-functions report shape (MIN/MAX/AVG/COUNT per country).</summary>
public class CountryStatsRow
{
    public string Country { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal AvgPrice { get; set; }
}
