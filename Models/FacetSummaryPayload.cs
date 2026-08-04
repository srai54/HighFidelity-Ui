namespace HighFidelity.Ui.Models;

/// <summary>Deserialization envelope for GET /api/reports/facet-summary — three reports in one response.</summary>
public class FacetSummaryPayload
{
    public List<OrderRow> TopOrdersByPrice { get; set; } = [];
    public List<CountryRevenueRow> RevenueByCountry { get; set; } = [];
    public List<CountryStatsRow> OrderStatsByCountry { get; set; } = [];
}
