using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Boolka;

public partial class MainWindow
{
    private readonly PreferencesStore preferencesStore;
    private AppPreferences preferences = new();
    private bool initializingPreferences = true;

    private void ReverseSort(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        preferences = preferences with { DescendingSort = !(preferences.DescendingSort ?? true) };
        SavePreferences();
        Refresh();
    }

    private void UpdateSortDirection()
    {
        var descending = preferences.DescendingSort ?? true;
        SortDirectionButton.Content = new LucideIcon { Icon = descending ? "arrow-down" : "arrow-up" };
        System.Windows.Automation.AutomationProperties.SetName(SortDirectionButton,
            UiText.Get(descending ? "По убыванию. Изменить направление сортировки" : "По возрастанию. Изменить направление сортировки"));
    }

    private void ShrinkCollection(object sender, RoutedEventArgs e) => ChangeCollectionScale(-0.1);
    private void EnlargeCollection(object sender, RoutedEventArgs e) => ChangeCollectionScale(0.1);

    private void ChangeCollectionScale(double delta)
    {
        if (busy) return;
        preferences = preferences with { CollectionScale = Math.Clamp(Math.Round(preferences.CollectionScale + delta, 2), 0.5, 1.5) };
        ApplyCollectionScale();
        SavePreferences();
    }

    private void ApplyCollectionScale()
    {
        CollectionScaleTransform.ScaleX = CollectionScaleTransform.ScaleY = preferences.CollectionScale;
        CollectionScaleLabel.Text = $"{preferences.CollectionScale:P0}";
        ShrinkCollectionButton.IsEnabled = !busy && preferences.CollectionScale > 0.5;
        EnlargeCollectionButton.IsEnabled = !busy && preferences.CollectionScale < 1.5;
        System.Windows.Automation.AutomationProperties.SetName(ShrinkCollectionButton, UiText.Get("Уменьшить элементы"));
        System.Windows.Automation.AutomationProperties.SetName(EnlargeCollectionButton, UiText.Get("Увеличить элементы"));
    }

    private void PresenceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AddonList is null) return;
        SavePreferences();
        Refresh();
    }

    private void SavePreferences()
    {
        if (initializingPreferences) return;
        preferences = preferences with { CatalogFilter = Math.Max(0, CatalogPresenceFilterBox.SelectedIndex), SortIndex = Math.Max(0, SortBox.SelectedIndex) };
        try { preferencesStore.Save(preferences); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            AppLog.Error("ui.operation.failed", ex);
            SetStatus(UiText.Get("Не удалось сохранить настройки отображения: ") + UiError.Describe(ex), needsAttention: true);
        }
    }

    private void AddonContextOpening(object sender, ContextMenuEventArgs e)
    {
        e.Handled = true;
        if (busy || catalogMode || sender is not ListBoxItem { DataContext: Addon addon } row) return;
        if (!row.IsSelected) AddonList.SelectedItem = addon;
        var menu = BuildAddonContextMenu(addon);
        menu.PlacementTarget = row;
        row.ContextMenu = menu;
        menu.IsOpen = true;
    }

    private ContextMenu BuildAddonContextMenu(Addon addon)
    {
        var menu = new ContextMenu();
        void Add(string title, bool enabled, RoutedEventHandler action)
        {
            var item = new MenuItem { Header = title, IsEnabled = enabled };
            item.Click += action;
            menu.Items.Add(item);
        }

        var selection = AddonList.SelectedItems.OfType<Addon>().ToArray();
        var identified = addon.PreviewId is > 0;
        Add(selection.Length > 1 ? UiText.Format($"Удалить выбранные ({selection.Length})") : UiText.Get("Удалить аддон"),
            !busy && selection.Length > 0, async (_, _) => await RemoveAddonsAsync(selection));
        if (selection.Length > 1) return menu;
        menu.Items.Add(new Separator { Style = (Style)FindResource("MenuSeparator") });
        Add(UiText.Get("Переустановить"), !busy && identified, async (_, _) => await ExecuteInstallationAsync(addon, false, reinstall: true));
        menu.Items.Add(new Separator { Style = (Style)FindResource("MenuSeparator") });
        Add(addon.UpdatesIgnored ? UiText.Get("Не игнорировать обновления") : UiText.Get("Игнорировать обновления"), !busy && identified, (_, _) => ToggleIgnored(addon));
        menu.Items.Add(new Separator { Style = (Style)FindResource("MenuSeparator") });
        Add(UiText.Get("Открыть страницу на ESOUI"), !busy && identified, (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo($"https://www.esoui.com/downloads/info{addon.PreviewId}") { UseShellExecute = true }); }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                AppLog.Error("ui.operation.failed", ex);
                SetStatus(UiText.Get("Не удалось открыть страницу: ") + UiError.Describe(ex), needsAttention: true);
            }
        });
        return menu;
    }

    private void ToggleIgnored(Addon addon)
    {
        if (busy || addon.PreviewId is not long id) return;
        var ignored = (preferences.IgnoredIds ?? []).ToHashSet();
        if (!ignored.Add(id)) ignored.Remove(id);
        preferences = preferences with { IgnoredIds = ignored.Order().ToArray() };
        SavePreferences();
        ApplyComparisons();
        Refresh();
        SetStatus(InstalledStatus());
    }

    private async void UpdateAll(object sender, RoutedEventArgs e)
    {
        if (!busy) await ExecuteInstallationAsync(null, false, batch: true);
    }

    private async void RepairAllLibraries(object sender, RoutedEventArgs e)
    {
        if (!busy) await ExecuteInstallationAsync(null, true, batch: true);
    }

    private void UpdateCollectionActions()
    {
        var available = !busy && currentFolder is not null && catalogLoadedAt.HasValue;
        var updates = installed.Where(x => x.HasUpdate && !x.UpdatesIgnored).Select(x => x.PreviewId).Distinct().Count();
        UpdateAllButton.IsEnabled = available && updates > 0;
        UpdateAllButton.Content = updates > 0 ? UiText.Format($"Обновить всё ({updates})") : UiText.Get("Обновить всё");
        ButtonState.Apply(UpdateAllButton, updates > 0 ? "Update" : null);
        var needsLibraries = installed.Any(x => x.DependencyHealth is { HasProblems: true, Unknown.Count: 0 } && x.PreviewId is > 0 && !x.UpdatesIgnored);
        RepairAllButton.IsEnabled = available && needsLibraries;
        ButtonState.Apply(RepairAllButton, needsLibraries ? "Warning" : null);
        var visibility = !catalogMode && DetailPage.Visibility != Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
        UpdateAllButton.Visibility = RepairAllButton.Visibility = visibility;
        ExportPackButton.Visibility = DetailPage.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        ImportPackButton.Visibility = DetailPage.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private async Task RemoveAddonsAsync(IReadOnlyList<Addon> addons)
    {
        if (busy || catalogMode || currentFolder is null || addons.Count == 0) return;
        var folders = addons.Select(addon => Path.GetFullPath(addon.Path)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var folder = folders[0];
        var target = currentFolder;
        var summary = UiText.Format($"Переместить папку аддона в резервную копию:\n{folder}\n\nОстальные аддоны и SavedVariables останутся на месте. Закройте ESO перед продолжением.");
        if (folders.Length > 1)
            summary = UiText.Format($"Переместить выбранные папки в резервную копию ({folders.Length})?\n{string.Join("\n", folders)}\n\nОстальные аддоны и SavedVariables останутся на месте. Закройте ESO перед продолжением.");
        if (new OperationConfirmationWindow(this, UiText.Get("Удаление аддона"), summary, destructive: true).ShowDialog() != true) return;
        SetBusy(true, UiText.Get("Перемещаем аддон в резервную копию…"));
        installCommitting = true;
        try
        {
            var result = await new SafeAddonRemoval().RemoveAsync(target, folders, lifetime.Token);
            lastBackupDirectory = result.BackupDirectory;
            OpenBackupButton.Visibility = Visibility.Visible;
            SetStatus(UiText.Format($"Удалено аддонов: {result.InstalledFolders.Count}. Резервная копия: {result.BackupDirectory}"));
            try
            {
                installed = (await ScanInstalledFolderAsync(target)).Addons;
                ApplyComparisons();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                AppLog.Error("ui.operation.failed", ex);
                SetStatus(StatusLabel.Text + UiText.Get(" · Не удалось обновить список: ") + UiError.Describe(ex), needsAttention: true);
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or OperationCanceledException)
        {
            AppLog.Error("ui.operation.failed", ex);
            SetStatus(UiText.Get("Удаление не завершено: ") + UiError.Describe(ex), needsAttention: true);
        }
        finally
        {
            installCommitting = false;
            SetBusy(false);
            Refresh();
        }
    }
}
