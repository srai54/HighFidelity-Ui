namespace HighFidelity.Ui.Components;

/// <summary>
/// Pulsing skeleton placeholder shown in place of a card while its data is
/// still loading (see MainPage.xaml's SummaryStrip/RevenueRowGrid shimmer
/// overlays, bound to MainViewModel.IsBusy).
/// </summary>
public partial class ShimmerBoxView : Border
{
    private const string PulseAnimationName = "ShimmerPulse";

    public ShimmerBoxView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        this.Animate(
            PulseAnimationName,
            new Animation(v => Opacity = v, 1.0, 0.4),
            length: 700,
            repeat: () => true,
            easing: Easing.SinInOut);
    }

    private void OnUnloaded(object? sender, EventArgs e) => this.AbortAnimation(PulseAnimationName);
}
