using System.Windows;
using System.Windows.Media;

namespace Boolka;

public static class ThemeManager
{
    public static Brush Brush(string color) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(ThemePalette.Map(color)));

    public static void Apply(string theme)
    {
        ThemePalette.IsDracula = theme == "dracula";
        ThemePalette.IsLight = theme is "light" or "classic" or "warm";
        ThemePalette.IsClassic = true;
        if (Application.Current is not { } app) return;
        foreach (var color in ThemeColors.Values)
            app.Resources["Theme." + color.TrimStart('#').ToUpperInvariant()] = Brush(color);
        app.Resources["Muted"] = Brush("#9AA3B5");
        // Эти ресурсы описывают состояние, а не исходный цвет конкретного элемента.
        foreach (var pair in new Dictionary<string, string>
        {
            ["Accent"] = ThemePalette.Accent, ["Update"] = ThemePalette.Update,
            ["Warning"] = ThemePalette.Warning, ["Error"] = ThemePalette.Error,
            ["Danger"] = ThemePalette.Danger, ["OnState"] = ThemePalette.OnState,
            ["SelectionBackground"] = ThemePalette.SelectionBackground
        }) app.Resources["State." + pair.Key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(pair.Value));
        app.Resources["State.Hover"] = ThemePalette.IsLight ? Brushes.Black : Brushes.White;
        app.Resources["StatusAttention"] = app.Resources["State.Error"];
        app.Resources["StatusAttentionText"] = app.Resources["State.OnState"];
        foreach (var radius in new[] { 4, 5, 7, 8, 10, 12 })
            app.Resources["Theme.Radius" + radius] = new CornerRadius(0);
        app.Resources["Theme.ButtonOutline"] = ThemePalette.IsClassic ? Brush("#353C4E") : Brushes.Transparent;
        foreach (Window window in app.Windows) WindowTheme.Refresh(window);
    }
}
