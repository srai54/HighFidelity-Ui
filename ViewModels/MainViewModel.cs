using System.Collections.ObjectModel;
using System.Timers;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighFidelity.Ui.Services.Interfaces;
using HighFidelity.Ui.Models;
using HighFidelity.Ui.Views;
using Timer = System.Timers.Timer;

namespace HighFidelity.Ui.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private const int PageSize = 5;
    // Real-time updates now arrive via IDocumentStatusHubClient; this timer is
    // just a safety net in case the SignalR connection is down, so it can be
    // much less frequent than the old 5-second poll.
    private const double DocumentPollIntervalMs = 60_000;

    private readonly IDashboardDataService _dataService;
    private readonly IDocumentStatusHubClient _documentStatusHub;
    private Timer? _documentPollTimer;
    private readonly IPrintService _printService;
    private readonly List<OrderModel> _allOrders = [];

    public ObservableCollection<DashboardCard> DashboardCards { get; } = [];
    public ObservableCollection<RevenueCardItem> RevenueCards { get; } = [];
    public ObservableCollection<ActivityModel> Activities { get; } = [];
    public ObservableCollection<OrderModel> Orders { get; } = [];
    public ObservableCollection<PageItem> PageNumbers { get; } = [];
    public ObservableCollection<TrafficModel> TrafficSources { get; } = [];
    public ObservableCollection<DocumentModel> Documents { get; } = [];
    public ObservableCollection<DeadLetterMessageModel> DeadLetterMessages { get; } = [];

    public DashboardCard? WalletCard => DashboardCards.ElementAtOrDefault(0);
    public DashboardCard? ReferralCard => DashboardCards.ElementAtOrDefault(1);
    public DashboardCard? EstimateCard => DashboardCards.ElementAtOrDefault(2);
    public DashboardCard? EarningCard => DashboardCards.ElementAtOrDefault(3);

    public RevenueCardItem? RevenueStatusCard => RevenueCards.ElementAtOrDefault(0);
    public RevenueCardItem? PageViewCard => RevenueCards.ElementAtOrDefault(1);
    public RevenueCardItem? BounceRateCard => RevenueCards.ElementAtOrDefault(2);
    public RevenueCardItem? RevenueStatusAltCard => RevenueCards.ElementAtOrDefault(3);

    public string CurrentMonthEarnings => "$3468.96";
    public string CurrentMonthSales => "82";
    public string DashboardDisplayTitle => "Dashboard";
    public string DashboardSubtitle => "Overview of Latest Month";
    public bool IsDataLoaded { get; private set; }

    [ObservableProperty]
    private string _documentStatusSummary = "";

    [ObservableProperty]
    private int _deadLetterCount;

#pragma warning disable MVVMTK0045 // ObservableProperty fields not AOT-compatible on WinRT
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _ordersFooterText = string.Empty;
#pragma warning restore MVVMTK0045

    private int _currentPage = 1;

    public MainViewModel(IDashboardDataService dataService, IPrintService printService, IDocumentStatusHubClient documentStatusHub)
    {
        _dataService = dataService;
        _printService = printService;
        _documentStatusHub = documentStatusHub;
        Title = DashboardDisplayTitle;

        // Primary path: push notifications from the API's DocumentStatusHub
        // (see HighFidelity-Api's DocumentStatusHub/InternalNotificationsController)
        // fire the instant HighFidelity.Functions finishes a status transition.
        _documentStatusHub.DocumentStatusChanged += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await LoadDocumentsAsync();
                await LoadDeadLetterMessagesAsync();
            });
        };

        // Fallback poll only, in case the hub connection is ever down.
        _documentPollTimer = new Timer(DocumentPollIntervalMs);
        _documentPollTimer.Elapsed += async (_, _) =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await LoadDocumentsAsync();
                await LoadDeadLetterMessagesAsync();
            });
        };
    }

    public async Task InitializeAsync()
    {
        if (IsDataLoaded) return;
        await LoadDataCommand.ExecuteAsync(null);
        IsDataLoaded = true;
        _documentPollTimer?.Start();
        await _documentStatusHub.StartAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        await Task.WhenAll(
            LoadDashboardCardsAsync(),
            LoadRevenueCardsAsync(),
            LoadActivitiesAsync(),
            LoadOrdersAsync(),
            LoadTrafficSourcesAsync(),
            LoadDocumentsAsync(),
            LoadDeadLetterMessagesAsync()
        );

        IsBusy = false;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsDataLoaded = false;
        await InitializeAsync();
    }

    [RelayCommand]
    private async Task NavigateAsync(MenuItemModel? item)
    {
        if (item is null) return;
        await Shell.Current.GoToAsync($"detail?title={item.Label}");
    }

    private static readonly IReadOnlyList<SummaryStat> LastMonthStats =
    [
        new("Earnings", "$2,980.44"),
        new("Sales", "74"),
        new("New Orders", "61"),
        new("Refunds", "3"),
        new("Top Seller", "Wireless Headset"),
        new("Best Region", "Italy"),
        new("Growth vs May", "+16.4%", Highlight: true),
    ];

    [RelayCommand]
    private async Task LastMonthSummaryAsync()
    {
        var page = Shell.Current.CurrentPage;
        if (page is null) return;
        await page.ShowPopupAsync(new LastMonthSummaryPopup(LastMonthStats));
    }

    // ---------- Order Status: search / pagination / toolbar ----------

    partial void OnSearchTextChanged(string value)
    {
        _currentPage = 1;
        RefreshOrdersView();
    }

    private IEnumerable<OrderModel> FilteredOrders =>
        string.IsNullOrWhiteSpace(SearchText)
            ? _allOrders
            : _allOrders.Where(o =>
                o.Customer.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                o.Country.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                o.Status.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                o.Invoice.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    private void RefreshOrdersView()
    {
        var filtered = FilteredOrders.ToList();
        int total = filtered.Count;
        int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        _currentPage = Math.Clamp(_currentPage, 1, totalPages);

        Orders.Clear();
        foreach (var order in filtered.Skip((_currentPage - 1) * PageSize).Take(PageSize))
            Orders.Add(order);

        PageNumbers.Clear();
        for (int page = 1; page <= totalPages; page++)
            PageNumbers.Add(new PageItem(page, page == _currentPage));

        int start = total == 0 ? 0 : (_currentPage - 1) * PageSize + 1;
        int end = Math.Min(_currentPage * PageSize, total);
        OrdersFooterText = $"Showing {start} to {end} of {total} entries";
    }

    [RelayCommand]
    private void GoToPage(PageItem? page)
    {
        if (page is null) return;
        _currentPage = page.Number;
        RefreshOrdersView();
    }

    [RelayCommand]
    private void NextPage()
    {
        _currentPage++;
        RefreshOrdersView();
    }

    [RelayCommand]
    private void PreviousPage()
    {
        _currentPage--;
        RefreshOrdersView();
    }

    // Each tap toggles that row independently, so several orders can be
    // selected at once (bulk selection) — across pages, since the flag
    // lives on the model.
    [RelayCommand]
    private void SelectOrder(OrderModel? order)
    {
        if (order is null) return;
        order.IsSelected = !order.IsSelected;
    }

    [RelayCommand]
    private async Task AddOrderAsync()
    {
        var page = Shell.Current.CurrentPage;
        if (page is null) return;
        var result = await page.ShowPopupAsync(new AddOrderPopup());
        if (result is (string customer, string country, decimal price, string status))
        {
            await AppendOrderAsync(customer, country, price, status);
        }
    }

    // Persists the order through the data service (the backend assigns Id and
    // Invoice), then appends it and jumps to the last page so the values the
    // user just typed are visible immediately.
    private async Task AppendOrderAsync(string customer, string country, decimal price, string status)
    {
        var result = await _dataService.AddOrderAsync(customer, country, price, status);
        if (result.IsFailure)
        {
            await Shell.Current.DisplayAlertAsync("Add Order", result.ErrorMessage, "OK");
            return;
        }

        _allOrders.Add(result.Data!);

        SearchText = string.Empty;
        _currentPage = int.MaxValue; // clamped to the last page by RefreshOrdersView
        RefreshOrdersView();
    }

    // Deletes every selected order (bulk delete) after a single confirmation.
    [RelayCommand]
    private async Task DeleteOrderAsync()
    {
        var selected = _allOrders.Where(o => o.IsSelected).ToList();
        if (selected.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync("Delete Orders", "Tap one or more rows to select the orders you want to delete.", "OK");
            return;
        }

        var page = Shell.Current.CurrentPage;
        if (page is null) return;
        var result = await page.ShowPopupAsync(new ConfirmDeletePopup(selected));
        if (result is not true) return;

        // Delete in the database first; only mirror in the UI on success.
        var deleteResult = await _dataService.DeleteOrdersAsync(selected.Select(o => o.Id).ToList());
        if (deleteResult.IsFailure)
        {
            await Shell.Current.DisplayAlertAsync("Delete Orders", deleteResult.ErrorMessage, "OK");
            return;
        }

        foreach (var order in selected)
            _allOrders.Remove(order);
        RefreshOrdersView();
    }

    [RelayCommand]
    private async Task ShowOrderInfoAsync()
    {
        var page = Shell.Current.CurrentPage;
        if (page is null) return;

        var filtered = FilteredOrders.ToList();
        int open = filtered.Count(o => o.Status == "Open");
        int process = filtered.Count(o => o.Status == "Process");
        int onHold = filtered.Count(o => o.Status == "On Hold");
        decimal totalValue = filtered.Sum(o => o.Price);

        await page.ShowPopupAsync(new OrderSummaryPopup(
            filtered.Count, open, process, onHold, totalValue,
            SearchText, _allOrders.Count));
    }

    [RelayCommand]
    private async Task UploadDocumentAsync()
    {
        try
        {
            var fileResult = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a document to upload"
            });

            if (fileResult is null) return;

            using var stream = await fileResult.OpenReadAsync();
            var result = await _dataService.UploadDocumentAsync(
                fileResult.FileName, stream, fileResult.ContentType ?? "application/octet-stream");

            if (result.IsFailure)
            {
                await Shell.Current.DisplayAlertAsync("Upload Failed", result.ErrorMessage, "OK");
                return;
            }

            Documents.Insert(0, result.Data!);

            await Shell.Current.DisplayAlertAsync(
                "Upload Successful",
                $"\"{fileResult.FileName}\" uploaded successfully.",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Upload Error", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task LoadDocumentsAsync()
    {
        var result = await _dataService.GetDocumentsAsync();
        if (result.IsFailure) return;

        Documents.Clear();
        int pending = 0, processing = 0, completed = 0, failed = 0;
        foreach (var doc in result.Data!)
        {
            Documents.Add(doc);
            switch (doc.ProcessingStatus)
            {
                case "Pending": pending++; break;
                case "Processing": processing++; break;
                case "Completed": completed++; break;
                case "Failed": failed++; break;
            }
        }

        DocumentStatusSummary = FormatDocumentSummary(pending, processing, completed, failed);
    }

    private static string FormatDocumentSummary(int pending, int processing, int completed, int failed)
    {
        var parts = new List<string>();
        if (completed > 0) parts.Add($"{completed} completed");
        if (processing > 0) parts.Add($"{processing} processing");
        if (pending > 0) parts.Add($"{pending} waiting");
        if (failed > 0) parts.Add($"{failed} failed");
        return parts.Count > 0 ? string.Join(", ", parts) : "No documents";
    }

    // A different signal from ProcessingStatus=="Failed" above: that's a SQL
    // row this app wrote; this is the Service Bus queue's own view of
    // messages it gave up on (see docs/BRANCHES.md's ancestor,
    // HighFidelity-Api's ServiceBusController) — a message can dead-letter
    // for reasons that have nothing to do with a specific document (e.g. the
    // document row no longer existing at all).
    [RelayCommand]
    private async Task LoadDeadLetterMessagesAsync()
    {
        var result = await _dataService.GetDeadLetterMessagesAsync();
        if (result.IsFailure) return;

        DeadLetterMessages.Clear();
        foreach (var message in result.Data!)
            DeadLetterMessages.Add(message);

        DeadLetterCount = DeadLetterMessages.Count;
    }

    [RelayCommand]
    private async Task PrintOrdersAsync()
    {
        var filtered = FilteredOrders.ToList();
        await _printService.PrintOrdersAsync(filtered, "OrderStatus_LatestMonth");
    }

    [RelayCommand]
    private async Task DownloadPdfAsync()
    {
        var selected = Orders.Where(o => o.IsSelected).ToList();
        if (selected.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync("No Selection",
                "Select orders by checking the boxes in the table.", "OK");
            return;
        }
        await _printService.PrintOrdersAsync(selected, "SelectedOrders");
    }

    // ---------- Data loading ----------

    private async Task LoadDashboardCardsAsync()
    {
        var result = await _dataService.GetDashboardCardsAsync();
        if (result.IsFailure) return;

        DashboardCards.Clear();
        foreach (var card in result.Data!)
            DashboardCards.Add(card);

        OnPropertyChanged(nameof(WalletCard));
        OnPropertyChanged(nameof(ReferralCard));
        OnPropertyChanged(nameof(EstimateCard));
        OnPropertyChanged(nameof(EarningCard));
    }

    private async Task LoadRevenueCardsAsync()
    {
        var result = await _dataService.GetRevenueCardsAsync();
        if (result.IsFailure) return;

        RevenueCards.Clear();
        foreach (var card in result.Data!)
            RevenueCards.Add(card);

        OnPropertyChanged(nameof(RevenueStatusCard));
        OnPropertyChanged(nameof(PageViewCard));
        OnPropertyChanged(nameof(BounceRateCard));
        OnPropertyChanged(nameof(RevenueStatusAltCard));
    }

    private async Task LoadActivitiesAsync()
    {
        var result = await _dataService.GetActivitiesAsync();
        if (result.IsFailure) return;

        Activities.Clear();
        foreach (var activity in result.Data!)
            Activities.Add(activity);
    }

    private async Task LoadOrdersAsync()
    {
        var result = await _dataService.GetOrdersAsync();
        if (result.IsFailure) return;

        _allOrders.Clear();
        _allOrders.AddRange(result.Data!);
        _currentPage = 1;
        RefreshOrdersView();
    }

    private async Task LoadTrafficSourcesAsync()
    {
        var result = await _dataService.GetTrafficSourcesAsync();
        if (result.IsFailure) return;

        TrafficSources.Clear();
        foreach (var source in result.Data!)
            TrafficSources.Add(source);
    }
}
