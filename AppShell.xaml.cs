using HighFidelity.Ui.Views;

namespace HighFidelity.Ui;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("detail", typeof(DetailPage));
		Routing.RegisterRoute("mongo-concepts", typeof(MongoConceptsPage));
	}
}
