# HighFidelity.Ui

A **.NET MAUI** dashboard that recreates the assignment screenshot with high visual fidelity, following the assignment's **MVVM folder structure**, with **Dependency Injection**, service interfaces, real cross-platform **printing**, and a **responsive layout** that adapts from desktop to phone (and 100%→400% zoom).

Runs on **Windows** and **Android** (iOS/macCatalyst targets included).

---

## Branches

| Branch | Purpose |
|---|---|
| `feature/enterprise-frontend-refactor` | **Active development line.** Enterprise EF Core backend integration, JWT auth, Documents upload feature, generic chart architecture. |
| `master` | GitHub's default branch. Frozen at the original pre-refactor static-data build (23 commits behind) — a historical checkpoint, not kept in sync. |
| `feature/backend-api-integration` | Historical — added the first Dapper/SQL LocalDB backend and wired the app to a live API. Fully superseded, an ancestor of the current mainline. |
| `backend-split` | Historical — a 4-commit, path-rewritten extraction of just the backend code, used to bootstrap the standalone [HighFidelity-Api](https://github.com/srai54/HighFidelity-Api) repo. An orphaned snapshot, disjoint from this repo's main history. |
| `feature/offline-demo-fallback` | Frozen checkpoint preserving the last commit with a static/embedded-data implementation (`StaticDashboardDataService`) — a fallback for demoing with no backend running, from before the mainline made the API a hard dependency. |
| `refactor/generic-chart-architecture` | Frozen checkpoint right after the config-driven chart refactor (`ChartData`/`ChartTheme`/`ChartGeometry` + reusable `IDrawable` chart types) landed — an ancestor of the mainline, not diverged. |
| `feature/interview-ready-ui` | Reset to the last commit before the Documents upload feature, checkbox column, and Download PDF button were added — kept for MAUI interview prep, showing the original dashboard only. |

---

## Project Structure (per assignment)

```
HighFidelity.Ui/
├── Views/                          # Pages
│   ├── MainPage.xaml               # Dashboard + responsive breakpoint logic
│   ├── DetailPage.xaml             # Sidebar navigation detail page
│   ├── PrintPreviewPage.xaml       # Report preview + OS print dialog
│   └── LastMonthSummaryPopup.xaml  # Structured stats popup
├── Components/                     # 8 reusable ContentViews
│   ├── SidebarView.xaml            # 200px dark navigation (hamburger overlay on phones)
│   ├── DashboardHeaderView.xaml    # Title, earnings, sales, summary button
│   ├── SalesChartView.xaml         # Spline chart + grid + period tabs (GraphicsView)
│   ├── TrafficChartView.xaml       # Donut chart with legend
│   ├── SummaryCardView.xaml        # KPI card (used 4x)
│   ├── RevenueCardView.xaml        # Analytics card with bold vector arrow (used 4x)
│   ├── ActivityTimelineView.xaml   # Vertical activity timeline
│   └── OrderTableView.xaml         # Orders: search, pagination, add/delete/print
├── ViewModels/
│   ├── BaseViewModel.cs            # ObservableObject base (IsBusy, Title)
│   ├── MainViewModel.cs            # Dashboard state + commands
│   └── DetailViewModel.cs
├── Models/                         # POCO models + Result<T> + SummaryStat
├── Services/
│   ├── Interfaces/                 # IDashboardDataService, IPrintService
│   ├── ApiDashboardDataService.cs      # HttpClient implementation (only data source on this branch)
│   ├── ApiSettings.cs                  # Backend base address (per-platform)
│   └── PrintService.cs             # HTML report + print preview flow
├── Converters/                     # 4 IValueConverters
├── Resources/Styles/               # Colors.xaml, Styles.xaml, Fonts.xaml
├── MauiProgram.cs                  # DI registrations
└── run.cmd                         # Interactive launcher: Windows or Android
```

**MVVM:** Views bind to ViewModels (CommunityToolkit.Mvvm source generators); ViewModels depend on service **interfaces**; code-behind is reserved for visual concerns (chart drawing, responsive re-layout, platform print calls).

---

## How to Run

> **This branch is pure frontend — it has no embedded/static data.** The app has nothing to show until [HighFidelity.Api](https://github.com/srai54/HighFidelity-Api) is running. Clone it as a sibling folder and start it first:
> ```powershell
> git clone https://github.com/srai54/HighFidelity-Api.git
> sqlcmd -S "(localdb)\MSSQLLocalDB" -d HighFidelity -i HighFidelity-Api\database\seed.sql
> dotnet run --project HighFidelity-Api\HighFidelity.Api
> ```
> (Want to run the FE standalone with no backend at all, e.g. for a quick demo? Use the `feature/offline-demo-fallback` branch instead, which keeps an in-memory data source.)

### Interactive picker (Windows or Android)
```bat
.\run.cmd
```
Choose `1` for Windows or `2` for Android. For Android it auto-starts the Pixel 7 emulator (software rendering) if no device is connected, waits for boot, then deploys.

### Direct commands
```bash
dotnet run -f net10.0-windows10.0.19041.0        # Windows
dotnet build -t:Run -f net10.0-android            # Android (device/emulator required)
```

### Visual Studio
Open the solution, pick the **framework** (`net10.0-windows…` or `net10.0-android`) and the device from the debug-target dropdown, press **F5**.

> **Emulator tip:** if the Android emulator boots to a black/stuck screen on this machine, start it with software rendering:
> `emulator -avd pixel_7_-_api_36_0 -gpu swiftshader_indirect` — `run.cmd` already does this. On first boot the emulator is slow; if an "isn't responding" dialog appears, choose **Wait**.

---

## Feature Highlights

| Feature | Where | Notes |
|---------|-------|-------|
| High-fidelity dashboard | `Views/MainPage.xaml` + `Components/` | Pixel-matched to the assignment screenshot |
| Custom charts | `SalesChartView`, `TrafficChartView`, `RevenueCardView` | `GraphicsView` + `IDrawable`, no chart library; full grid behind splines |
| Bold trend arrows | `RevenueCardView` | Stroked vector `Path` (text `↑` ignores bold — fallback-font glyph) |
| Real printing | `Services/PrintService`, `Views/PrintPreviewPage` | Windows: WebView2 print dialog (printers + PDF). Android: system `PrintManager` |
| Structured summary popup | `Views/LastMonthSummaryPopup` | CommunityToolkit Popup, label/value rows, highlighted growth |
| Responsive layout | `MainPage.xaml.cs` (`ApplyResponsiveLayout`) | <980px: sidebar → hamburger overlay, single-column stacking, 2×2 KPI grid, revenue cards wrap 4/2/1, order table pans horizontally |
| Orders toolbar | `OrderTableView` + `MainViewModel` | Search, pagination, add (manual/quick), delete, info summary, print |

---

## Architecture Notes

- **DI:** everything is registered in `MauiProgram.cs`; ViewModels/pages get dependencies via constructor.
- **`Result<T>`:** every service call returns `Success(data)` or `Failure(error)` — explicit error handling, no exception-driven flow.
- **Parallel loading:** `MainViewModel.InitializeAsync` fetches all five data sets with `Task.WhenAll`.
- **Swappable data source, backed by an interface:** `IDashboardDataService` has exactly one implementation on this branch — `ApiDashboardDataService`, registered in `MauiProgram.cs`:

```csharp
builder.Services.AddSingleton<IDashboardDataService>(_ =>
    new ApiDashboardDataService(new HttpClient
    {
        BaseAddress = new Uri(ApiSettings.BaseAddress),
        Timeout = ApiSettings.RequestTimeout
    }));
```

Because ViewModels only depend on the interface, an alternate implementation (in-memory, cached, a different backend) is a one-line DI swap away without touching a single ViewModel or XAML file — see the `feature/offline-demo-fallback` branch for a working example (`StaticDashboardDataService`).

Endpoints consumed: `GET/POST/DELETE /api/dashboard/{cards|revenue-cards|activities|orders|traffic}`, `GET /health` — see [HighFidelity-Api](https://github.com/srai54/HighFidelity-Api) for the backend implementation.

---

## Docs

- [docs/interview-architecture.md](docs/interview-architecture.md) — architecture walkthrough, data/print flow, design decisions
- [docs/explanation-video-detailed.md](docs/explanation-video-detailed.md) — detailed explanation video script: intro → all UI features → code flow

## Built With

- .NET 10 / MAUI
- CommunityToolkit.Mvvm (source-generated MVVM)
- CommunityToolkit.Maui (Popup)
- GraphicsView + IDrawable custom charts
