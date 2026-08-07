using System.Windows.Input;
using HighFidelity.Ui.Models;

namespace HighFidelity.Ui.Components;

public partial class SidebarView : ContentView
{
    public static readonly BindableProperty SelectItemCommandProperty =
        BindableProperty.Create(nameof(SelectItemCommand), typeof(ICommand), typeof(SidebarView));

    public ICommand? SelectItemCommand
    {
        get => (ICommand?)GetValue(SelectItemCommandProperty);
        set => SetValue(SelectItemCommandProperty, value);
    }

    // Font Awesome solid glyphs (fa-solid-900.ttf, family "FontAwesome")
    public List<MenuItemModel> MenuItems { get; } =
    [
        new() { Label = "Dashboard",      Icon = "", IsActive = true, Route = "dashboard" }, // house
        new() { Label = "Widgets",        Icon = "", Route = "widgets" },        // table-cells-large
        new() { Label = "UI Elements",    Icon = "", Route = "uielements" },     // layer-group
        new() { Label = "Advanced UI",    Icon = "", Route = "advancedui" },     // wand-magic-sparkles
        new() { Label = "Form Elements",  Icon = "", Route = "formelements" },   // pen-to-square
        new() { Label = "Editors",        Icon = "", Route = "editors" },        // pen-nib
        new() { Label = "Charts",         Icon = "", Route = "charts" },         // chart-pie
        new() { Label = "Tables",         Icon = "", Route = "tables" },         // table
        new() { Label = "Popups",         Icon = "", Route = "popups" },         // window-restore
        new() { Label = "Notifications",  Icon = "", Route = "notifications" },  // bell
        new() { Label = "Icons",          Icon = "", Route = "icons" },          // star
        new() { Label = "Maps",           Icon = "", Route = "maps" },           // location-dot
        new() { Label = "User Pages",     Icon = "", Route = "userpages" },      // user
        new() { Label = "Error Pages",    Icon = "", Route = "errorpages" },     // triangle-exclamation
        new() { Label = "General Pages",  Icon = "", Route = "generalpages" },   // file-lines
        new() { Label = "E-Commerce",     Icon = "", Route = "ecommerce" },      // cart-shopping
        new() { Label = "E-mail",         Icon = "", Route = "email" },          // envelope
        new() { Label = "Calendar",       Icon = "", Route = "calendar" },       // calendar-days
        new() { Label = "Todo List",      Icon = "", Route = "todolist" },       // list-check
        new() { Label = "Gallery",        Icon = "", Route = "gallery" },        // images
        new() { Label = "Documentation",  Icon = "", Route = "documentation" },  // book
        new() { Label = "Documents",      Icon = "", Route = "documents" },      // paperclip
    ];

    public SidebarView()
    {
        InitializeComponent();
        UpdateThemeToggleLabel();
    }

    /// <summary>
    /// Pushes the menu items down so the floating hamburger button sits above
    /// the first item (used when the sidebar overlays content on narrow screens).
    /// </summary>
    public void SetTopInset(double inset)
        => MenuStack.Padding = new Thickness(0, 10 + inset, 0, 10);

    private void OnThemeToggleTapped(object? sender, TappedEventArgs e)
    {
        var app = Application.Current;
        if (app is null) return;

        var isDark = app.RequestedTheme == AppTheme.Dark;
        app.UserAppTheme = isDark ? AppTheme.Light : AppTheme.Dark;
        UpdateThemeToggleLabel();
    }

    private void UpdateThemeToggleLabel()
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        ThemeToggleLabel.Text = isDark ? "Light Mode" : "Dark Mode";
        ThemeToggleIcon.Text = isDark ? "" : ""; // sun / moon
    }
}
