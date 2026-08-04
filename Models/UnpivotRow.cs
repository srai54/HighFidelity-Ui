namespace HighFidelity.Ui.Models;

/// <summary>UNPIVOT result shape — one row per (Country, Status) pair.</summary>
public class UnpivotRow
{
    public string Country { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}
