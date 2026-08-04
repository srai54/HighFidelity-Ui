using HighFidelity.Ui.ViewModels;

namespace HighFidelity.Ui.Views;

public partial class MongoConceptsPage : ContentPage
{
    public MongoConceptsPage(MongoConceptsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
