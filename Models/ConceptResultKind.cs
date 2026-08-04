namespace HighFidelity.Ui.Models;

/// <summary>
/// Which typed table/list a ConceptResultItem's response should render as.
/// Message covers anything better shown as text: the explain() diagnostic
/// (a document, not a table), validation-rejection/status messages, and
/// anything that fails to parse as its declared shape.
/// </summary>
public enum ConceptResultKind
{
    Message,
    Orders,
    CountryRevenue,
    CountryStats,
    RankedOrders,
    EmployeeChain,
    CustomerInsights,
    Pivot,
    Unpivot,
    PagedOrders,
    Facet
}
