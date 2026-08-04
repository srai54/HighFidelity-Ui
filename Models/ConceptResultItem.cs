using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighFidelity.Ui.Services.Interfaces;

namespace HighFidelity.Ui.Models;

/// <summary>
/// One row on the MongoDB-concepts demo page: a topic label, the endpoint it
/// calls, its own Run command, and — based on <see cref="Kind"/> — a typed
/// table/list of the parsed response rather than a raw JSON dump. Falls
/// back to showing the raw response text if parsing the declared shape
/// fails, or for shapes that are genuinely better read as text/a document
/// (the explain() diagnostic, status/validation messages).
/// </summary>
public partial class ConceptResultItem : ObservableObject
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IApiProbeService _api;
    private readonly string _path;
    private readonly object? _postBody;
    private readonly bool _isPost;

    public string Topic { get; }
    public string MethodLabel { get; }
    public string Note { get; }
    public ConceptResultKind Kind { get; }

    [ObservableProperty]
    private string resultText = "Tap Run to call this endpoint.";

    [ObservableProperty]
    private string statusLine = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    private bool hasTypedResult;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPagedInfo))]
    private string pagedInfo = string.Empty;

    public ObservableCollection<OrderRow> Orders { get; } = [];
    public ObservableCollection<CountryRevenueRow> CountryRevenues { get; } = [];
    public ObservableCollection<CountryStatsRow> CountryStatsRows { get; } = [];
    public ObservableCollection<RankedOrderRow> RankedOrders { get; } = [];
    public ObservableCollection<EmployeeChainRow> EmployeeChain { get; } = [];
    public ObservableCollection<CustomerInsightRow> CustomerInsights { get; } = [];
    public ObservableCollection<PivotRow> PivotRows { get; } = [];
    public ObservableCollection<UnpivotRow> UnpivotRows { get; } = [];

    // Computed rather than [ObservableProperty] — each is just "does this
    // specific collection have rows", raised manually via NotifyHasFlags()
    // whenever the collections are cleared/populated. Lets the XAML hide a
    // shape's header row when a card doesn't use that shape, without a
    // value converter.
    public bool HasOrders => Orders.Count > 0;
    public bool HasCountryRevenues => CountryRevenues.Count > 0;
    public bool HasCountryStats => CountryStatsRows.Count > 0;
    public bool HasRankedOrders => RankedOrders.Count > 0;
    public bool HasEmployeeChain => EmployeeChain.Count > 0;
    public bool HasCustomerInsights => CustomerInsights.Count > 0;
    public bool HasPivotRows => PivotRows.Count > 0;
    public bool HasUnpivotRows => UnpivotRows.Count > 0;
    public bool HasPagedInfo => !string.IsNullOrEmpty(PagedInfo);

    /// <summary>True whenever there's no typed table to show — either this card is Message-kind, the response failed, or parsing the declared shape didn't produce any rows.</summary>
    public bool ShowRawFallback => !HasTypedResult;

    partial void OnHasTypedResultChanged(bool value) => OnPropertyChanged(nameof(ShowRawFallback));

    public ConceptResultItem(IApiProbeService api, string topic, string path, ConceptResultKind kind = ConceptResultKind.Message,
        string note = "", bool isPost = false, object? postBody = null)
    {
        _api = api;
        _path = path;
        _isPost = isPost;
        _postBody = postBody;
        Topic = topic;
        Kind = kind;
        MethodLabel = isPost ? "POST" : "GET";
        Note = note;
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        IsBusy = true;
        IsError = false;
        HasTypedResult = false;
        ClearTypedCollections();
        NotifyHasFlags();

        var result = _isPost ? await _api.PostAsync(_path, _postBody) : await _api.GetAsync(_path);

        if (result.IsFailure)
        {
            ResultText = result.ErrorMessage!;
            StatusLine = "Request failed";
            IsError = true;
            IsBusy = false;
            return;
        }

        var raw = result.Data!;
        var (statusLinePart, bodyPart) = SplitStatusAndBody(raw);
        StatusLine = statusLinePart;
        ResultText = raw;
        IsError = !statusLinePart.Contains("200") && !statusLinePart.Contains("204");

        if (!IsError && Kind != ConceptResultKind.Message)
            HasTypedResult = TryRenderTyped(bodyPart);

        NotifyHasFlags();
        IsBusy = false;
    }

    private void NotifyHasFlags()
    {
        OnPropertyChanged(nameof(HasOrders));
        OnPropertyChanged(nameof(HasCountryRevenues));
        OnPropertyChanged(nameof(HasCountryStats));
        OnPropertyChanged(nameof(HasRankedOrders));
        OnPropertyChanged(nameof(HasEmployeeChain));
        OnPropertyChanged(nameof(HasCustomerInsights));
        OnPropertyChanged(nameof(HasPivotRows));
        OnPropertyChanged(nameof(HasUnpivotRows));
    }

    private static (string StatusLine, string Body) SplitStatusAndBody(string raw)
    {
        var separatorIndex = raw.IndexOf("\n\n", StringComparison.Ordinal);
        return separatorIndex < 0 ? (raw, string.Empty) : (raw[..separatorIndex], raw[(separatorIndex + 2)..]);
    }

    private void ClearTypedCollections()
    {
        Orders.Clear();
        CountryRevenues.Clear();
        CountryStatsRows.Clear();
        RankedOrders.Clear();
        EmployeeChain.Clear();
        CustomerInsights.Clear();
        PivotRows.Clear();
        UnpivotRows.Clear();
        PagedInfo = string.Empty;
    }

    private bool TryRenderTyped(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;

        try
        {
            switch (Kind)
            {
                case ConceptResultKind.Orders:
                    return AddAll(Orders, JsonSerializer.Deserialize<List<OrderRow>>(body, JsonOptions));

                case ConceptResultKind.CountryRevenue:
                    return AddAll(CountryRevenues, JsonSerializer.Deserialize<List<CountryRevenueRow>>(body, JsonOptions));

                case ConceptResultKind.CountryStats:
                    return AddAll(CountryStatsRows, JsonSerializer.Deserialize<List<CountryStatsRow>>(body, JsonOptions));

                case ConceptResultKind.RankedOrders:
                    return AddAll(RankedOrders, JsonSerializer.Deserialize<List<RankedOrderRow>>(body, JsonOptions));

                case ConceptResultKind.EmployeeChain:
                    return AddAll(EmployeeChain, JsonSerializer.Deserialize<List<EmployeeChainRow>>(body, JsonOptions));

                case ConceptResultKind.CustomerInsights:
                    return AddAll(CustomerInsights, JsonSerializer.Deserialize<List<CustomerInsightRow>>(body, JsonOptions));

                case ConceptResultKind.Pivot:
                    return AddAll(PivotRows, JsonSerializer.Deserialize<List<PivotRow>>(body, JsonOptions));

                case ConceptResultKind.Unpivot:
                    return AddAll(UnpivotRows, JsonSerializer.Deserialize<List<UnpivotRow>>(body, JsonOptions));

                case ConceptResultKind.PagedOrders:
                    var paged = JsonSerializer.Deserialize<PagedOrdersPayload>(body, JsonOptions);
                    if (paged is null) return false;
                    PagedInfo = $"Page {paged.Page} of {paged.TotalPages} · {paged.TotalCount} total";
                    return AddAll(Orders, paged.Items);

                case ConceptResultKind.Facet:
                    var facet = JsonSerializer.Deserialize<FacetSummaryPayload>(body, JsonOptions);
                    if (facet is null) return false;
                    var gotOrders = AddAll(Orders, facet.TopOrdersByPrice);
                    var gotRevenue = AddAll(CountryRevenues, facet.RevenueByCountry);
                    var gotStats = AddAll(CountryStatsRows, facet.OrderStatsByCountry);
                    return gotOrders || gotRevenue || gotStats;

                default:
                    return false;
            }
        }
        catch (JsonException)
        {
            // Response didn't match the declared shape (e.g. an error body) —
            // ResultText already holds the raw text as a fallback.
            return false;
        }
    }

    private static bool AddAll<T>(ObservableCollection<T> target, List<T>? items)
    {
        if (items is null || items.Count == 0) return false;
        foreach (var item in items) target.Add(item);
        return true;
    }
}
