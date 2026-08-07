using HighFidelity.Ui.Models;
using HighFidelity.Ui.Services.Interfaces;
using HighFidelity.Ui.ViewModels;
using Moq;

namespace HighFidelity.Ui.Tests;

public class MainViewModelTests
{
    private readonly Mock<IDashboardDataService> _dataService = new();
    private readonly Mock<IPrintService> _printService = new();
    private readonly MainViewModel _sut;

    public MainViewModelTests()
    {
        _dataService.Setup(s => s.GetDashboardCardsAsync())
            .ReturnsAsync(Result<IReadOnlyList<DashboardCard>>.Success([]));
        _dataService.Setup(s => s.GetRevenueCardsAsync())
            .ReturnsAsync(Result<IReadOnlyList<RevenueCardItem>>.Success([]));
        _dataService.Setup(s => s.GetActivitiesAsync())
            .ReturnsAsync(Result<IReadOnlyList<ActivityModel>>.Success([]));
        _dataService.Setup(s => s.GetTrafficSourcesAsync())
            .ReturnsAsync(Result<IReadOnlyList<TrafficModel>>.Success([]));
        _dataService.Setup(s => s.GetDeadLetterMessagesAsync())
            .ReturnsAsync(Result<IReadOnlyList<DeadLetterMessageModel>>.Success([]));
        _dataService.Setup(s => s.GetDocumentsAsync())
            .ReturnsAsync(Result<IReadOnlyList<DocumentModel>>.Success([]));
        _dataService.Setup(s => s.GetOrdersAsync())
            .ReturnsAsync(Result<IReadOnlyList<OrderModel>>.Success(SampleOrders(12)));

        _sut = new MainViewModel(_dataService.Object, _printService.Object);
    }

    private static List<OrderModel> SampleOrders(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new OrderModel
            {
                Id = i,
                Invoice = 10000 + i,
                Customer = $"Customer {i}",
                Country = i % 2 == 0 ? "USA" : "Canada",
                Price = i * 10,
                Status = "Open"
            })
            .ToList();

    [Fact]
    public async Task LoadDataCommand_PopulatesOrders_PagedToFirstFive()
    {
        await _sut.LoadDataCommand.ExecuteAsync(null);

        Assert.Equal(5, _sut.Orders.Count);
        Assert.Equal("Showing 1 to 5 of 12 entries", _sut.OrdersFooterText);
        Assert.Equal(3, _sut.PageNumbers.Count); // ceil(12/5) = 3
    }

    [Fact]
    public async Task SearchText_FiltersOrders_ByCountry()
    {
        await _sut.LoadDataCommand.ExecuteAsync(null);

        _sut.SearchText = "USA";

        // 6 of the 12 sample orders are USA (even-numbered) — one page's worth (PageSize=5) plus 1 on page 2
        Assert.All(_sut.Orders, o => Assert.Equal("USA", o.Country));
        Assert.Equal("Showing 1 to 5 of 6 entries", _sut.OrdersFooterText);
        Assert.Equal(2, _sut.PageNumbers.Count); // ceil(6/5) = 2
    }

    [Fact]
    public async Task SearchText_Reset_RestoresFullFirstPage()
    {
        await _sut.LoadDataCommand.ExecuteAsync(null);
        _sut.SearchText = "Customer 1";
        _sut.SearchText = "";

        Assert.Equal(5, _sut.Orders.Count);
        Assert.Equal("Showing 1 to 5 of 12 entries", _sut.OrdersFooterText);
    }

    [Fact]
    public async Task LoadDocumentsAsync_BuildsStatusSummary_FromProcessingStatuses()
    {
        _dataService.Setup(s => s.GetDocumentsAsync()).ReturnsAsync(
            Result<IReadOnlyList<DocumentModel>>.Success(
            [
                new DocumentModel { Id = 1, FileName = "a.pdf", ProcessingStatus = "Completed" },
                new DocumentModel { Id = 2, FileName = "b.pdf", ProcessingStatus = "Completed" },
                new DocumentModel { Id = 3, FileName = "c.pdf", ProcessingStatus = "Processing" },
                new DocumentModel { Id = 4, FileName = "d.pdf", ProcessingStatus = "Failed" },
            ]));

        await _sut.LoadDocumentsCommand.ExecuteAsync(null);

        Assert.Equal(4, _sut.Documents.Count);
        Assert.Equal("2 completed, 1 processing, 1 failed", _sut.DocumentStatusSummary);
    }

    [Fact]
    public async Task LoadDeadLetterMessagesAsync_SetsCount()
    {
        _dataService.Setup(s => s.GetDeadLetterMessagesAsync()).ReturnsAsync(
            Result<IReadOnlyList<DeadLetterMessageModel>>.Success(
            [
                new DeadLetterMessageModel { MessageId = "m1" },
                new DeadLetterMessageModel { MessageId = "m2" },
            ]));

        await _sut.LoadDeadLetterMessagesCommand.ExecuteAsync(null);

        Assert.Equal(2, _sut.DeadLetterCount);
    }
}
