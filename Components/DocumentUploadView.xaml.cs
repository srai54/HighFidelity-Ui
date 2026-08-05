using System.Collections.ObjectModel;
using System.Windows.Input;
using HighFidelity.Ui.Models;

namespace HighFidelity.Ui.Components;

public partial class DocumentUploadView : ContentView
{
    public static readonly BindableProperty DocumentsProperty =
        BindableProperty.Create(nameof(Documents), typeof(ObservableCollection<DocumentModel>), typeof(DocumentUploadView));

    public static readonly BindableProperty UploadDocumentCommandProperty =
        BindableProperty.Create(nameof(UploadDocumentCommand), typeof(ICommand), typeof(DocumentUploadView));

    public static readonly BindableProperty DocumentStatusSummaryProperty =
        BindableProperty.Create(nameof(DocumentStatusSummary), typeof(string), typeof(DocumentUploadView), "Ready");

    public static readonly BindableProperty DeadLetterCountProperty =
        BindableProperty.Create(nameof(DeadLetterCount), typeof(int), typeof(DocumentUploadView), 0);

    public ObservableCollection<DocumentModel> Documents
    {
        get => (ObservableCollection<DocumentModel>)GetValue(DocumentsProperty);
        set => SetValue(DocumentsProperty, value);
    }

    public ICommand? UploadDocumentCommand
    {
        get => (ICommand?)GetValue(UploadDocumentCommandProperty);
        set => SetValue(UploadDocumentCommandProperty, value);
    }

    public string DocumentStatusSummary
    {
        get => (string)GetValue(DocumentStatusSummaryProperty);
        set => SetValue(DocumentStatusSummaryProperty, value);
    }

    /// <summary>Count of messages sitting in the backend's Service Bus dead-letter sub-queue — a different signal from any single document's own "Failed" status.</summary>
    public int DeadLetterCount
    {
        get => (int)GetValue(DeadLetterCountProperty);
        set => SetValue(DeadLetterCountProperty, value);
    }

    public DocumentUploadView()
    {
        InitializeComponent();
    }
}
