using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighFidelity.Ui.Services.Interfaces;

namespace HighFidelity.Ui.Models;

/// <summary>
/// One row on the MongoDB-concepts demo page: a topic label, the endpoint it
/// calls, and its own Run command so each row can be triggered and re-run
/// independently — tapping one doesn't block or reset any of the others.
/// </summary>
public partial class ConceptResultItem : ObservableObject
{
    private readonly IApiProbeService _api;
    private readonly string _path;
    private readonly object? _postBody;
    private readonly bool _isPost;

    public string Topic { get; }
    public string MethodLabel { get; }
    public string Note { get; }

    [ObservableProperty]
    private string resultText = "Tap Run to call this endpoint.";

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isError;

    public ConceptResultItem(IApiProbeService api, string topic, string path, string note = "", bool isPost = false, object? postBody = null)
    {
        _api = api;
        _path = path;
        _isPost = isPost;
        _postBody = postBody;
        Topic = topic;
        MethodLabel = isPost ? "POST" : "GET";
        Note = note;
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        IsBusy = true;
        IsError = false;

        var result = _isPost ? await _api.PostAsync(_path, _postBody) : await _api.GetAsync(_path);

        ResultText = result.IsSuccess ? result.Data! : result.ErrorMessage!;
        IsError = result.IsFailure;
        IsBusy = false;
    }
}
