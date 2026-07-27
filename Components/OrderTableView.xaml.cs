namespace HighFidelity.Ui.Components;

public partial class OrderTableView : ContentView
{
    // The table needs at least this much width to show all columns;
    // below it (phones) the table pans horizontally instead of clipping.
    private const double MinTableWidth = 520;

    private bool _toolbarNarrow;
    private bool _nudged;

    public OrderTableView()
    {
        InitializeComponent();
        TableScroll.SizeChanged += (_, _) =>
        {
            if (TableScroll.Width <= 0) return;
            TableContent.WidthRequest = Math.Max(TableScroll.Width, MinTableWidth);

            // The table only pans when it is wider than its viewport; surface
            // that with a hint plus a one-time nudge so the scroll is discoverable.
            bool pannable = TableScroll.Width < MinTableWidth;
            PanHint.IsVisible = pannable;
            if (pannable && !_nudged)
            {
                _nudged = true;
                Dispatcher.Dispatch(async () =>
                {
                    try
                    {
                        await Task.Delay(600);
                        await TableScroll.ScrollToAsync(70, 0, true);
                        await Task.Delay(150);
                        await TableScroll.ScrollToAsync(0, 0, true);
                    }
                    catch
                    {
                        // Purely cosmetic; never crash over a hint animation.
                    }
                });
            }
        };
        OrdersToolbar.SizeChanged += (_, _) => ApplyToolbarLayout();
    }

    // Below this width the icon buttons and the 150px search field can't share one
    // row without overlapping (seen on phones), so the search drops to a second row.
    private void ApplyToolbarLayout()
    {
        if (OrdersToolbar.Width <= 0) return;
        bool narrow = OrdersToolbar.Width < 430;
        if (narrow == _toolbarNarrow) return;
        _toolbarNarrow = narrow;

        if (narrow)
        {
            Grid.SetRow(SearchEntry, 1);
            Grid.SetColumn(SearchEntry, 0);
            Grid.SetColumnSpan(SearchEntry, 8);
            SearchEntry.WidthRequest = -1;
            SearchEntry.HorizontalOptions = LayoutOptions.Fill;
        }
        else
        {
            Grid.SetRow(SearchEntry, 0);
            Grid.SetColumn(SearchEntry, 5);
            Grid.SetColumnSpan(SearchEntry, 1);
            SearchEntry.WidthRequest = 150;
            SearchEntry.HorizontalOptions = LayoutOptions.End;
        }
    }
}
