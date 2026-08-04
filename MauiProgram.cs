using CommunityToolkit.Maui;
using HighFidelity.Ui.ViewModels;
using HighFidelity.Ui.Services.Interfaces;
using HighFidelity.Ui.Services;
using HighFidelity.Ui.Views;
using Microsoft.Extensions.Logging;
#if WINDOWS
using Microsoft.Maui.LifecycleEvents;
#endif

namespace HighFidelity.Ui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("fa-solid-900.ttf", "FontAwesome");
            });

#if ANDROID
        // Android's Material Entry draws an underline in every state; remove it so
        // search/form fields match the flat design (they sit in their own Borders).
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
        {
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
        });
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
        {
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
        });
#endif

#if WINDOWS
        // WinUI paints an accent-blue underline on focused text boxes; the app-level
        // theme overrides in Platforms/Windows/App.xaml don't reach it, so override the
        // resource per control. Must run after Loaded — inserting into a ResourceDictionary
        // before the control joins the visual tree crashes with COMException 0x800F0902.
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("FlatFocusVisual", (handler, view) =>
        {
            var textBox = handler.PlatformView;
            textBox.Loaded += (s, e) =>
            {
                try
                {
                    var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
                    // Rest state too — otherwise the underline shows until the pointer hovers.
                    textBox.Resources["TextControlBorderThemeThickness"] = new Microsoft.UI.Xaml.Thickness(0);
                    textBox.Resources["TextControlBorderThemeThicknessFocused"] = new Microsoft.UI.Xaml.Thickness(0);
                    textBox.Resources["TextControlBorderBrush"] = transparent;
                    textBox.Resources["TextControlBorderBrushFocused"] = transparent;
                    textBox.Resources["TextControlBorderBrushPointerOver"] = transparent;
                    textBox.Resources["TextControlBorderBrushDisabled"] = transparent;
                    textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                }
                catch
                {
                    // Purely cosmetic; never crash the app over a focus visual.
                }
            };
        });

        // Launch maximized so the dashboard opens at its designed proportions.
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(windows => windows.OnWindowCreated(window =>
            {
                var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                    presenter.Maximize();
            }));
        });
#endif

        // Data Service — backed by HighFidelity.Api, a separate repo/deployable:
        // https://github.com/srai54/HighFidelity-Api
        // Clone it alongside this repo and run it first: dotnet run --project HighFidelity.Api
        // This branch has no embedded/static data — the app has nothing to show
        // until the API is running. See Services/ApiSettings.cs for the base
        // address the FE resolves per platform.
        //
        // The API now requires a JWT (see HighFidelity-Api/docs/ARCHITECTURE.md),
        // so requests go through a plain client used only to log in, and a second
        // client whose AuthTokenHandler attaches the resulting Bearer token.
        var authHttpClient = new HttpClient
        {
            BaseAddress = new Uri(ApiSettings.BaseAddress),
            Timeout = ApiSettings.RequestTimeout
        };
        // DelegatingHandler requires an InnerHandler to actually send the request
        // after its own logic runs — HttpClient's constructor does NOT wire this
        // up automatically, so without it every real call throws
        // "The inner handler has not been assigned" (caught and swallowed by
        // ApiDashboardDataService's try/catch, which is why this failed silently
        // with blank data instead of a visible error).
        var authTokenHandler = new AuthTokenHandler(authHttpClient) { InnerHandler = new HttpClientHandler() };
        // Shared by both services below so there's only ever one AuthTokenHandler
        // (one login flow) — a second HttpClient(authTokenHandler) would attach
        // the same handler instance to two clients, which is fine for reads but
        // means every additional client shares one shared token cache anyway,
        // so there's no reason not to just share the HttpClient itself.
        var authenticatedHttpClient = new HttpClient(authTokenHandler)
        {
            BaseAddress = new Uri(ApiSettings.BaseAddress),
            Timeout = ApiSettings.RequestTimeout
        };
        builder.Services.AddSingleton<IDashboardDataService>(_ => new ApiDashboardDataService(authenticatedHttpClient));
        builder.Services.AddSingleton<IPrintService, PrintService>();

        // MongoDB-concepts demo page (feature/mongo-fe-implementation) — calls
        // HighFidelity-Api's feature/mongodbnoazure-advanced ReportsController
        // endpoints directly and displays the raw response, to visually prove
        // each SQL-interview-topic implementation actually works. See that
        // repo's docs/MONGODB_ADVANCED_FEATURES.md for the backend-side writeup.
        builder.Services.AddSingleton<IApiProbeService>(_ => new ApiProbeService(authenticatedHttpClient));
        builder.Services.AddTransient<MongoConceptsViewModel>();
        builder.Services.AddTransient<MongoConceptsPage>();

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<DetailViewModel>();
        builder.Services.AddTransient<DetailPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
