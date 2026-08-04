namespace HighFidelity.Ui.Models;

/// <summary>Window-functions + CASE + UDF report shape.</summary>
public class RankedOrderRow
{
    public int Id { get; set; }
    public string Customer { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int RankInCountry { get; set; }
    public int DenseRankInCountry { get; set; }
    public decimal RunningTotalInCountry { get; set; }
    public string PriceTierCase { get; set; } = string.Empty;
    public string PriceTierUdf { get; set; } = string.Empty;
}
