using HighFidelity.Ui.Models;
using HighFidelity.Ui.ViewModels;

namespace HighFidelity.Ui.Views;

public partial class MongoConceptsPage : ContentPage
{
    public MongoConceptsPage(MongoConceptsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}

/// <summary>
/// Picks exactly ONE card template per ConceptResultItem, based on its Kind.
/// The first version stacked all 8 possible table shapes inside every card
/// (each hidden via IsVisible unless it matched), which meant 23 cards x 8
/// nested BindableLayouts existed in the visual tree at once — verified to
/// reliably crash WinUI's native renderer during scroll (Microsoft.UI.Xaml.dll,
/// exception 0xc000027b, identical fault address on repeat crashes — not a
/// managed exception, not app logic). This selector means each card builds
/// only the one table it actually needs.
/// </summary>
public class ConceptCardTemplateSelector : DataTemplateSelector
{
    public DataTemplate MessageTemplate { get; set; } = null!;
    public DataTemplate OrdersTemplate { get; set; } = null!;
    public DataTemplate CountryRevenueTemplate { get; set; } = null!;
    public DataTemplate CountryStatsTemplate { get; set; } = null!;
    public DataTemplate RankedOrdersTemplate { get; set; } = null!;
    public DataTemplate EmployeeChainTemplate { get; set; } = null!;
    public DataTemplate CustomerInsightsTemplate { get; set; } = null!;
    public DataTemplate PivotTemplate { get; set; } = null!;
    public DataTemplate UnpivotTemplate { get; set; } = null!;
    public DataTemplate FacetTemplate { get; set; } = null!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) =>
        ((ConceptResultItem)item).Kind switch
        {
            ConceptResultKind.Orders or ConceptResultKind.PagedOrders => OrdersTemplate,
            ConceptResultKind.CountryRevenue => CountryRevenueTemplate,
            ConceptResultKind.CountryStats => CountryStatsTemplate,
            ConceptResultKind.RankedOrders => RankedOrdersTemplate,
            ConceptResultKind.EmployeeChain => EmployeeChainTemplate,
            ConceptResultKind.CustomerInsights => CustomerInsightsTemplate,
            ConceptResultKind.Pivot => PivotTemplate,
            ConceptResultKind.Unpivot => UnpivotTemplate,
            ConceptResultKind.Facet => FacetTemplate,
            _ => MessageTemplate
        };
}
