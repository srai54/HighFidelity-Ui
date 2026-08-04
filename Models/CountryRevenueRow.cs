namespace HighFidelity.Ui.Models;

/// <summary>Revenue-by-country report shape — used by the "stored procedure"/HAVING card and the MERGE snapshot card.</summary>
public class CountryRevenueRow
{
    public string Country { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int OrderCount { get; set; }
}
