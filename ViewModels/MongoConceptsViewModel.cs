using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using HighFidelity.Ui.Models;
using HighFidelity.Ui.Services.Interfaces;

namespace HighFidelity.Ui.ViewModels;

/// <summary>
/// Backs the MongoDB-concepts demo page: one ConceptResultItem per endpoint
/// on HighFidelity.Api's feature/mongodbnoazure-advanced branch (see that
/// repo's docs/MONGODB_ADVANCED_FEATURES.md for the full topic-by-topic
/// writeup this page is a visual companion to). Numbering matches that doc's
/// section numbers, so a topic here and its explanation there are easy to
/// cross-reference.
/// </summary>
public partial class MongoConceptsViewModel : BaseViewModel
{
    private readonly IApiProbeService _api;

    public ObservableCollection<ConceptResultItem> Items { get; } = [];

    public MongoConceptsViewModel(IApiProbeService api)
    {
        _api = api;
        Title = "MongoDB Concepts";
        BuildItems();
    }

    private void BuildItems()
    {
        Items.Add(new(_api, "1. Joins ($lookup)", "api/reports/orders-with-customer-info"));
        Items.Add(new(_api, "2. Views (createView)", "api/reports/high-value-orders"));
        Items.Add(new(_api, "3. \"Stored procedure\" report", "api/reports/revenue-by-country"));
        Items.Add(new(_api, "3/15. GROUP BY + HAVING", "api/reports/revenue-by-country?minRevenue=2000", "Only countries with revenue over 2000"));
        Items.Add(new(_api, "4. Index + explain()", "api/reports/explain/orders-by-status/Open"));
        Items.Add(new(_api, "5. Constraints ($jsonSchema)", "api/reports/orders-with-integrity-check", "Try an invalid Status — rejected by the validator", true,
            new { customer = "Schema Test", country = "Nowhere", price = 10, status = "NotAValidStatus" }));
        Items.Add(new(_api, "6. Aggregate functions", "api/reports/order-stats-by-country"));
        Items.Add(new(_api, "6. Parameterized \"stored procedure\"", "api/reports/top-orders-by-country/Japan?topN=2"));
        Items.Add(new(_api, "7. Window functions + CASE + UDF", "api/reports/orders-ranked-by-country"));
        Items.Add(new(_api, "8. $facet (CTE equivalent)", "api/reports/facet-summary"));
        Items.Add(new(_api, "9. Recursive CTE — walk up", "api/reports/employees/Tomasz%20Nowak/management-chain"));
        Items.Add(new(_api, "9. Recursive CTE — walk down", "api/reports/employees/Aria%20Chen/reports"));
        Items.Add(new(_api, "10. Uncorrelated subquery", "api/reports/orders-above-average"));
        Items.Add(new(_api, "11. Correlated subquery + let", "api/reports/customer-order-insights"));
        Items.Add(new(_api, "12. PIVOT", "api/reports/order-status-pivot"));
        Items.Add(new(_api, "12. UNPIVOT", "api/reports/order-status-unpivot"));
        Items.Add(new(_api, "13. Dynamic query + IN", "api/reports/orders-search?status=Open&countries=Japan&countries=Italy&minPrice=1000"));
        Items.Add(new(_api, "15. UNION ALL", "api/reports/orders-union-all"));
        Items.Add(new(_api, "15. UNION (distinct)", "api/reports/orders-union-distinct"));
        Items.Add(new(_api, "15. Pagination", "api/reports/orders-paged?page=2&pageSize=5"));
        Items.Add(new(_api, "16. MERGE / temp table — read", "api/reports/snapshot/revenue", "Already populated at API startup"));
        Items.Add(new(_api, "16. MERGE / temp table — re-materialize", "api/reports/snapshot/materialize-revenue", "Safe to re-run — replaces, doesn't append", true));
        Items.Add(new(_api, "14. FK + application trigger", "api/reports/orders-with-integrity-check", "Creates a real Order + Customer + Activity row", true,
            new { customer = "Demo Customer", country = "Demoland", price = 999.99m, status = "Open" }));
        Items.Add(new(_api, "17. Transactions (needs a replica set)", "api/reports/transactions-demo/void-order/999999", "Expect HTTP 501 on this standalone MongoDB", true));
    }

    [RelayCommand]
    private async Task RunAllSafeAsync()
    {
        foreach (var item in Items.Where(i => i.MethodLabel == "GET"))
            await item.RunCommand.ExecuteAsync(null);
    }
}
