using System.IO;
using System.Windows;

namespace Boolka;

public partial class MainWindow
{
    private void OpenSettings(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        SettingsWindow? dialog = null;
        dialog = new SettingsWindow(this, preferences, ApplyPresentation, () =>
        {
            if (currentFolder is null) return;
            var target = currentFolder;
            var backups = new BackupsWindow(dialog!, target, value =>
            {
                installCommitting = value;
                SetBusy(value);
            }, async () =>
            {
                try
                {
                    installed = (await ScanInstalledFolderAsync(target)).Addons;
                    ApplyComparisons();
                    Refresh();
                    SetStatus(catalogMode ? CatalogStatus() : InstalledStatus());
                }
                catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
                {
                    AppLog.Error("ui.operation.failed", ex);
                    SetStatus(UiText.Get("Копия восстановлена, но не удалось пересканировать папку: ") + UiError.Describe(ex), needsAttention: true);
                }
            });
            backups.ShowDialog();
        }, currentFolder is not null, () => SaveDiagnostics(dialog!), () => OpenLaunchSettings(dialog), enabled =>
        {
            preferences = preferences with { AutoUpdateAddonsOnStartup = enabled };
            SavePreferences();
        });
        dialog.ShowDialog();
    }

    private void ApplyPresentation(string language, string theme)
    {
        preferences = preferences with { Language = language, Theme = theme, SquareCorners = true };
        UiResources.Apply(language);
        UpdateCategoryOptions();
        ThemeManager.Apply(theme);
        UpdateApplicationButton();
        UpdateLaunchButton();
        UpdateFolderChangeButton();
        ApplyCollectionScale();
        ApplyComparisons();
        Refresh();
        SetStatus(catalogMode ? CatalogStatus() : InstalledStatus());
        SavePreferences();
    }
}
