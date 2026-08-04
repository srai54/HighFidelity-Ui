namespace HighFidelity.Ui.Models;

/// <summary>PIVOT result shape — one row per country, one column per Status.</summary>
public class PivotRow
{
    public string Country { get; set; } = string.Empty;
    public int Open { get; set; }
    public int Process { get; set; }
    public int OnHold { get; set; }
}
