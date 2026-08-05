namespace HighFidelity.Ui.Models;

/// <summary>One message sitting in the backend's Service Bus dead-letter sub-queue — see IDashboardDataService.GetDeadLetterMessagesAsync.</summary>
public class DeadLetterMessageModel
{
    public string MessageId { get; set; } = string.Empty;
    public long SequenceNumber { get; set; }
    public DateTime EnqueuedTimeUtc { get; set; }
    public string? DeadLetterReason { get; set; }
    public string? DeadLetterErrorDescription { get; set; }
    public int? DocumentId { get; set; }
    public string? FileName { get; set; }

    public string EnqueuedDateDisplay => EnqueuedTimeUtc.ToString("MMM dd, yyyy HH:mm");
}
