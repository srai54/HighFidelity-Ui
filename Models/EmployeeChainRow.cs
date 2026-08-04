namespace HighFidelity.Ui.Models;

/// <summary>One node in a $graphLookup management-chain/reports result.</summary>
public class EmployeeChainRow
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ManagerId { get; set; }
}
