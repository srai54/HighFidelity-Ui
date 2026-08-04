# MongoDB Concepts Page — What It Is, How It's Built, What Was Fixed

Single reference doc for the `feature/mongo-fe-implementation` branch. This branch adds one page to the app: a live, visual proof that every SQL-interview-topic endpoint on the backend's `feature/mongodbnoazure-advanced` branch (see that repo's `docs/MONGODB_ADVANCED_FEATURES.md`) actually works — not just docs, not just curl output, but the real app calling the real API and rendering the real response.

---

## 1. What's on the page

A new sidebar entry, **"Mongo Concepts"** (database icon, bottom of the list) → a page with **23 cards**, one per backend endpoint. Each card has:

- A method badge (`GET`/`POST`) and topic label matching the backend doc's numbering (e.g. "9. Recursive CTE — walk up") so a card here and its writeup there are easy to cross-reference
- Its own **Run** button — tapping it calls the real endpoint against whichever backend is running on `localhost:5199` and renders the actual response
- A **typed table/list** matching the response shape (not raw JSON) — see §3
- A "Run all safe reads" button at the top that fires every `GET` card at once (the 3 `POST` cards are left for manual, deliberate taps since two of them have real side effects — see §2)

## 2. The 23 cards, what each proves, and any side effects

**Which cards are Views, and which are Stored Procedures — pointed out explicitly, since MongoDB has a real equivalent for one and only an equivalent-in-spirit for the other:**

- **View → card #2 ("Views (`createView`)") only.** This is the one card backed by an actual MongoDB **view object** — `HighValueOrdersView`, created once by the backend via the `createView` command (`MongoDbContext.EnsureIndexesAndViewsAsync` on the backend repo) and then queried by this card **exactly like a normal collection**. A real, first-class database object, same as a SQL view.
- **Stored Procedure → cards #3 ("'Stored procedure' report") and #6 ("Parameterized 'stored procedure'") only.** MongoDB has **no actual stored-procedure feature** (legacy server-side JS via `db.eval()` was deprecated for security reasons — see the backend's `docs/MONGODB_PERFORMANCE.md`). These two cards are the closest equivalent: report logic expressed as an **aggregation pipeline**, run from application code on demand instead of stored inside the database. Card #6 is the more convincing one — it actually takes runtime parameters (`country`, `topN`), the way a real `sp_TopOrdersByCountry(@Country, @TopN)` would.
- Every other card is a different SQL concept entirely (joins, subqueries, window functions, PIVOT, transactions, etc.) — none of the remaining 20 are Views or Stored Procedures, even though a few sound adjacent (`$facet` is the CTE equivalent, not a stored procedure; the MERGE/snapshot cards are the temp-table equivalent, not a view — a view is never materialized, and the snapshot collection very much is).

| # | Topic | Method | Side effects |
|---|---|---|---|
| 1 | Joins (`$lookup`) | GET | none |
| 2 | **Views** (`createView`) | GET | none |
| 3 | **"Stored procedure"** report | GET | none |
| 3/15 | GROUP BY + HAVING | GET | none |
| 4 | Index + `explain()` | GET | none |
| 5 | Constraints (`$jsonSchema`) | POST | Sends an invalid `Status` on purpose — rejected before it reaches MongoDB, by the same app-layer validation `orders-with-integrity-check` always runs (**not** actually a demo of the DB-level `$jsonSchema` check firing — see the card's own note text for why) |
| 6 | Aggregate functions | GET | none |
| 6 | Parameterized **"stored procedure"** | GET | none |
| 7 | Window functions + CASE + UDF | GET | none |
| 8 | `$facet` (CTE equivalent) | GET | none |
| 9 | Recursive CTE — walk up | GET | none |
| 9 | Recursive CTE — walk down | GET | none |
| 10 | Uncorrelated subquery | GET | none |
| 11 | Correlated subquery + `let` | GET | none |
| 12 | PIVOT | GET | none |
| 12 | UNPIVOT | GET | none |
| 13 | Dynamic query + IN | GET | none |
| 15 | UNION ALL | GET | none |
| 15 | UNION (distinct) | GET | none |
| 15 | Pagination | GET | none |
| 16 | MERGE / temp table — read | GET | none (reads the snapshot the backend already materializes at startup) |
| 16 | MERGE / temp table — re-materialize | POST | Safe to re-run — `$merge` replaces, doesn't append |
| 14 | FK + application trigger | POST | **Creates a real `Order` + `Customer` + `Activity` row every time you tap it.** Intentional — it's the only way to show the FK-upsert + audit-trigger behavior live. Uses a fixed "Demo Customer"/"Demoland" identity, so repeated taps just add more demo rows, never break anything existing. |
| 17 | Transactions (needs a replica set) | POST | none — targets order id `999999`, which never exists, and the standalone MongoDB rejects the transaction before ever looking it up. Expect `HTTP 501`. |

"Run all safe reads" only iterates cards where `MethodLabel == "GET"`, so it never touches the two side-effecting POSTs.

## 3. Architecture — how a card goes from a URL to a rendered table

```
MongoConceptsPage.xaml            ← DataTemplateSelector picks ONE table template per card (see §5)
    ↕ BindableLayout, no code-behind logic
MongoConceptsViewModel            ← builds the 23 ConceptResultItem instances, one per endpoint
    ↓ constructor injection
IApiProbeService / ApiProbeService ← calls the endpoint, returns Result<string> (raw HTTP status + body text)
    ↓ (inside ConceptResultItem.RunAsync)
ConceptResultItem                 ← parses the body into a typed row list based on its own Kind
    ↓ System.Text.Json.Deserialize<List<TRow>>
Models/*Row.cs                    ← OrderRow, CountryRevenueRow, RankedOrderRow, EmployeeChainRow, etc.
```

**`IApiProbeService`** (`Services/ApiProbeService.cs`) is deliberately separate from `IDashboardDataService` — this page calls a different backend controller (`ReportsController`, not `DashboardController`) and has completely different response shapes; reusing the dashboard service would mean bolting 20+ unrelated methods onto an interface every other page also depends on. It shares the **same authenticated `HttpClient`** as `IDashboardDataService` though (see `MauiProgram.cs`) — one login flow, not two.

**`ConceptResultItem`** (`Models/ConceptResultItem.cs`) is the per-card state: topic, path, `Kind` (which shape to expect), its own `RunCommand`, and one `ObservableCollection<TRow>` per possible shape (`Orders`, `CountryRevenues`, `RankedOrders`, etc. — only the one matching `Kind` ever gets populated). `RunAsync`:
1. Calls the endpoint via `IApiProbeService`
2. Splits the raw response into a status line (`"HTTP 200 OK"`) and a body
3. If `Kind != Message`, tries to `JsonSerializer.Deserialize` the body into that shape's row list
4. Falls back to showing the raw body as plain text if parsing fails, or if `Kind == Message` (used for `explain()`'s diagnostic document and plain status/error responses — genuinely better read as text than forced into a table)

**Why `Kind` is decided per-card, not inferred from the response:** the ViewModel already knows exactly what shape each endpoint returns (it's calling one specific URL, not an arbitrary one) — inferring it from the JSON at runtime would be solving a problem that doesn't exist here and would make failures harder to diagnose (a shape mismatch would look like "parsing failed" instead of "wrong Kind assigned").

## 4. The row models (`Models/*.cs`)

One class per response shape, deserialized with `JsonSerializerOptions(JsonSerializerDefaults.Web)` (case-insensitive, camelCase — matches the backend's `PropertyNamingPolicy.CamelCase` without needing `[JsonPropertyName]` on every property):

| Model | Shape | Used by |
|---|---|---|
| `OrderRow` | Id, Invoice, Customer, Country, Price, Status, CustomerSince? | Joins, views, subqueries, union, search, pagination, top-N, FK+trigger |
| `CountryRevenueRow` | Country, TotalRevenue, OrderCount | "Stored procedure" report, HAVING, MERGE snapshot |
| `CountryStatsRow` | Country, OrderCount, MinPrice, MaxPrice, AvgPrice | Aggregate functions |
| `RankedOrderRow` | + RankInCountry, DenseRankInCountry, RunningTotalInCountry, PriceTierCase, PriceTierUdf | Window functions + CASE + UDF |
| `EmployeeChainRow` | Name, Title, ManagerId? | Recursive CTE (up/down) |
| `CustomerInsightRow` | Customer, Country, Price, CountryAveragePrice | Correlated subquery |
| `PivotRow` | Country, Open, Process, OnHold | PIVOT |
| `UnpivotRow` | Country, Status, Count | UNPIVOT |
| `PagedOrdersPayload` | Items: List\<OrderRow\>, Page, PageSize, TotalCount, TotalPages | Pagination (envelope only — `Items` feeds the same `Orders` collection `OrderRow` cards use) |
| `FacetSummaryPayload` | TopOrdersByPrice, RevenueByCountry, OrderStatsByCountry | `$facet` (envelope only — populates all three existing collections at once) |

## 5. Why there's a `DataTemplateSelector` instead of one universal card template

The first version of this page used **one** `DataTemplate` for every card, containing all 8 possible table blocks stacked on top of each other, each hidden with `IsVisible` unless it matched that card's `Kind`. This compiled, looked right when static, and rendered correctly on first load.

**It reliably crashed the app during scrolling.** Confirmed via Windows Event Viewer (`Get-WinEvent -LogName Application`): a native `APPCRASH` inside `Microsoft.UI.Xaml.dll` (WinUI's own renderer, not managed .NET code), exception code `0xc000027b`, the **exact same fault address** on every repeat. Root cause: 23 cards × 8 nested `BindableLayout`s each (most invisible but still fully present in the visual tree) pushed WinUI's native renderer into an unstable state.

**The fix** (`Views/MongoConceptsPage.xaml.cs` → `ConceptCardTemplateSelector`, a `DataTemplateSelector` subclass): one `DataTemplate` per `ConceptResultKind`, each containing only the ONE table shape that `Kind` needs. `BindableLayout.ItemTemplateSelector` (not `ItemTemplate`) picks the right one per card. This means a card's markup is duplicated across ~10 templates instead of factored once — MAUI has no template-composition mechanism, so this is mechanical but low-risk, since each duplicated block is the exact same markup that was already visually verified working in the single-template version.

**Open item, not silently claimed as fully resolved:** after this fix, `Run`/`Run all safe reads` no longer crash. However, the same native crash still occurred once during *automated* testing while simulating mouse-wheel scroll input (`mouse_event` with `MOUSEEVENTF_WHEEL`) — even after the fix. A keyboard-based scroll test (Page Down, with every other window minimized so nothing could steal the keystrokes) survived cleanly, which points at the synthetic input method itself rather than the app. This was not cleanly proven either way — **scroll through the page yourself with a real mouse/trackpad** to confirm before considering this fully closed.

## 6. How to run and verify it yourself

1. Check out `feature/mongodbnoazure-advanced` in the backend repo (`HighFidelity-Api`) and run it: `dotnet run --project HighFidelity.Api` (listens on `:5199`)
2. Check out `feature/mongo-fe-implementation` here and run the Windows app (`run.cmd`, or `dotnet build -f net10.0-windows10.0.19041.0 -t:Run`)
3. Scroll the sidebar to the bottom → **Mongo Concepts**
4. Tap **Run all safe reads**, or tap **Run** on any individual card
5. Shortcut for scripted verification: launch with the environment variable `DASH_TEST_MONGO=1` set — jumps straight to this page on startup, same pattern as the existing `DASH_TEST_DETAIL`/`DASH_TEST_PRINT` hooks in `Views/MainPage.xaml.cs`

## 7. What did NOT change

No existing page, ViewModel, service, or converter was modified beyond two small, additive touches: one `if` branch in `MainViewModel.NavigateAsync` (routes to `"mongo-concepts"` when that specific sidebar item is tapped; every other item's routing is untouched) and one new `Routing.RegisterRoute` line in `AppShell.xaml.cs`. `IDashboardDataService`/`ApiDashboardDataService` — what every other page depends on — is completely untouched; `IApiProbeService` is a separate, additive service.
