namespace HighFidelity.Ui.Models;

public class DocumentModel
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public string ProcessingStatus { get; set; } = "Pending";
    public string? ProcessingError { get; set; }

    public string FileSizeDisplay => FileSize switch
    {
        < 1024 => $"{FileSize} B",
        < 1024 * 1024 => $"{FileSize / 1024.0:F1} KB",
        _ => $"{FileSize / (1024.0 * 1024):F1} MB"
    };

    public string UploadedDateDisplay => UploadedAtUtc.ToString("MMM dd, yyyy HH:mm");
}
