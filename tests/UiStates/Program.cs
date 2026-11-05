using Boolka;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml.Linq;

internal static class Program
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    [STAThread] static void Main()
    {
        var app = new Application();
        // Загружаем словарь без App/StartupUri, журналов и обработчиков запуска.
        var xml = XDocument.Load("src/Boolka.App/App.xaml");
        var ns = xml.Root!.Name.Namespace;
        var dictionary = new XElement(ns + "ResourceDictionary", new XAttribute("xmlns", ns.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml"),
            new XAttribute(XNamespace.Xmlns + "local", "clr-namespace:Boolka;assembly=boolka"),
            xml.Root.Element(ns + "Application.Resources")!.Elements());
        app.Resources = (ResourceDictionary)XamlReader.Parse(dictionary.ToString()
            .Replace("clr-namespace:Boolka\"", "clr-namespace:Boolka;assembly=boolka\""));
        var fixture = Path.Combine(AppContext.BaseDirectory, "fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(fixture);
        var watchedRoot = Path.Combine(fixture, "AddOns");
        bool Relevant(string relative, WatcherChangeTypes change = WatcherChangeTypes.Changed) =>
            AddonFolderChangeFilter.IsRelevant(watchedRoot, Path.Combine(watchedRoot, relative), change);
        Check(!Relevant(@"TamrielTradeCentre\PriceTable.lua"), "price Lua changes ignored");
        Check(!Relevant(@"TamrielTradeCentre\PriceTable.txt"), "price text changes ignored");
        Check(!Relevant(@"SavedVariables\TTC.txt", WatcherChangeTypes.Created), "saved variables ignored");
        Check(!Relevant(@"TTC\Cache\prices.json", WatcherChangeTypes.Created), "runtime data creation ignored");
        Check(!Relevant("TTC"), "directory last-write notifications ignored");
        Check(Relevant("TTC", WatcherChangeTypes.Created) && Relevant("TTC", WatcherChangeTypes.Deleted), "addon addition and deletion detected");
        Check(Relevant("TTC", WatcherChangeTypes.Renamed), "addon rename detected");
        Check(Relevant(@"TTC\TTC.txt") && Relevant(@"TTC\TTC.addon", WatcherChangeTypes.Deleted), "manifest change and deletion detected");
        Check(Relevant(@"TTC\Libs\LibFixture\LibFixture.TXT"), "nested library manifest detected");
        Check(!Relevant(@"..\Outside\Outside.txt"), "outside paths ignored");
        var legacyPath=Path.Combine(fixture,"legacy.preferences.json");
        File.WriteAllText(legacyPath,"""{"CompactRows":false,"PreviewMode":"images","TileColumns":8,"Language":"ru","Theme":"graphite","CollectionScale":1.2,"IgnoredIds":[12],"AutoUpdateAddonsOnStartup":true}""");
        var store=new PreferencesStore(legacyPath);var legacy=store.Load();
        Check(legacy.Language=="ru" && legacy.Theme=="graphite" && legacy.CollectionScale==1.2 && legacy.IgnoredIds!.SequenceEqual(new long[]{12}) && legacy.AutoUpdateAddonsOnStartup,"legacy settings preserve unrelated preferences");
        Check(store.NeedsViewPreferenceCleanup,"legacy view fields request one-time cleanup");
        var startupPath=Path.Combine(fixture,"startup.json");
        var startupPreferences=Path.ChangeExtension(startupPath,"preferences.json");
        File.Copy(legacyPath,startupPreferences);
        var migratedWindow=new MainWindow(new SettingsStore(startupPath),false);
        using(var startupDocument=System.Text.Json.JsonDocument.Parse(File.ReadAllText(startupPreferences)))
        {
            Check(!startupDocument.RootElement.TryGetProperty("PreviewMode",out _) && !startupDocument.RootElement.TryGetProperty("TileColumns",out _),"startup removes obsolete settings from disk");
            Check(startupDocument.RootElement.GetProperty("Language").GetString()=="ru" && startupDocument.RootElement.GetProperty("AutoUpdateAddonsOnStartup").GetBoolean(),"startup cleanup preserves unrelated settings");
        }
        store.Save(legacy);
        Check(!store.NeedsViewPreferenceCleanup,"successful save clears cleanup flag");
        using(var document=System.Text.Json.JsonDocument.Parse(File.ReadAllText(legacyPath)))
            foreach(var key in new[]{"CompactRows","PreviewMode","TileColumns"})
                Check(!document.RootElement.TryGetProperty(key,out _),"removed preference is not persisted: "+key);
        foreach(var type in new[]{"Boolka.AdaptiveTilePanel","Boolka.PreviewThumbnail","Boolka.PreviewService"})
            Check(typeof(MainWindow).Assembly.GetType(type) is null,"obsolete view code absent from assembly");
        var window = new MainWindow(new SettingsStore(Path.Combine(fixture, "settings.json")), false);
        var sample = new Addon("Fixture addon", "1", "Tester", "", Path.Combine(fixture, "Fixture"), "")
            { CatalogId = 1, CategoryId = 24, CategoryName = "Map, Coords, Compasses", Downloads = 1000000, IsInstalled = true, HasUpdate = true };
        var remote = sample with { Version = "2", IsInstalled = false, HasUpdate = false };
        sample = sample with { Comparison = new(sample, remote, UpdateStatus.UpdateAvailable, "Fixture") };
        Check(sample.HasVersionComparison && sample.PreviousVersionText == "1" && sample.AvailableVersionText == "2", "local and catalog versions stay distinct");
        Check(!(sample with { UpdatesIgnored = true }).HasVersionComparison, "ignored addon has no green version comparison");
        Check(!(sample with { Comparison = null }).HasVersionComparison, "unverified comparison is never invented");
        foreach (var language in new[] { "ru", "en", "de", "fr", "es", "pt-BR", "pl", "zh-CN", "ja" })
        {
            UiText.Language = language;
            Check(UiText.Get("Boolka · Настройки").Contains(" | "), "localized information separator " + language);
            Check(VersionLabel.Compact(sample) == "1", "compact localized version " + language);
            var buildOnly = sample with { Version = "", BuildVersion = "42" };
            Check(VersionLabel.Compact(buildOnly).Contains("42"), "build fallback retained");
        }
        UiText.Language = "en";
        Check(sample.AuthorByline.StartsWith(" | "), "author uses vertical separator");
        var caption = (TextBlock)window.FindName("CaptionLabel");
        var captionBackground = (Border)window.FindName("SidebarCaptionBackground");
        var scaleTransform = (ScaleTransform)window.FindName("InterfaceScale");
        var fullCaption = new TextBlock { Text = caption.Text, FontFamily = caption.FontFamily, FontSize = caption.FontSize, FontWeight = caption.FontWeight };
        fullCaption.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
        foreach (var scale in new[] {0.85,1.0,1.15})
        {
            scaleTransform.ScaleX = scaleTransform.ScaleY = scale;
            Invoke(window, "UpdateSidebarCaptionWidth");
            var root = (FrameworkElement)window.Content;
            root.Measure(new Size(1280,840));root.Arrange(new Rect(0,0,1280,840));root.UpdateLayout();
            Check(caption.ActualWidth + 0.01 >= fullCaption.DesiredSize.Width, "full title fits collapsed caption at scale " + scale);
            var bounds = caption.TransformToAncestor(captionBackground).TransformBounds(new Rect(0,0,caption.ActualWidth,caption.ActualHeight));
            Check(bounds.Right <= captionBackground.ActualWidth, "title stays inside dark caption strip");
        }
        scaleTransform.ScaleX = scaleTransform.ScaleY = 1;
        Invoke(window, "UpdateSidebarCaptionWidth");
        var bread = (Image)window.FindName("CaptionBread");
        Check(bread.Source is not null && bread.Visibility == Visibility.Visible, "bread visible in upper title bar");
        Invoke(window, "ToggleSidebar", window, new RoutedEventArgs());
        Check(((TextBlock)window.FindName("SidebarBrand")).Visibility == Visibility.Visible && Grid.GetRow((Button)window.FindName("SidebarToggle")) == 0, "expanded bread identity and toggle share header row");
        Check(sample.CategoryText == "Map, Coords, Compasses" && sample.AuthorByline.Contains("Tester"), "provider category and author retained");
        Check(!Logical(window).OfType<Image>().Any(image => image.Name == "SidebarBread"), "no duplicate bread in lower brand");
        var settings=new SettingsWindow(window,new AppPreferences(),(_,_)=>{},()=>{},false);
        Check(Logical(settings).OfType<ComboBox>().Count()==2,"settings expose language and theme only");
        Check(!Logical(settings).OfType<TextBlock>().Any(label => label.Text == UiText.Get("Настройки")), "duplicate settings heading removed");
        var settingsContent = (FrameworkElement)settings.Content;
        settingsContent.Measure(new Size(520, 640));
        settingsContent.Arrange(new Rect(0, 0, 520, 640));
        settingsContent.UpdateLayout();
        var hints = Logical(settings).OfType<TextBlock>().Where(label =>
        {
            var text = new System.Windows.Documents.TextRange(label.ContentStart, label.ContentEnd).Text;
            return text.StartsWith("(") && text.EndsWith(")");
        }).ToArray();
        Check(hints.Length == 2, "both settings hints in parentheses");
        foreach (var hint in hints)
        {
            Check(Grid.GetColumn(hint) == 1 && hint.TextWrapping == TextWrapping.Wrap && hint.ActualWidth > 100, "settings hint wraps to right of button");
        }

        Check(!Logical(settings).OfType<TextBlock>().Any(label=>label.Text==UiText.Get("Изображения модов")),"image preference removed from settings");
        var warning = sample with { HasUpdate = false, DependencyHealth = new(["LibFixture"], [], []) };
        foreach (var theme in new[] { "dracula", "graphite", "warm" })
        {
            ThemeManager.Apply(theme);
            Check(ThemePalette.Accent != ThemePalette.Error && ThemePalette.Accent != ThemePalette.Update, "distinct semantic colors");
            foreach (var state in new[] { "Update", "Warning", "Error", "Danger", "Accent" })
                Check(Contrast(ColorOf(app.Resources["State." + state]), ColorOf(app.Resources["State.OnState"])) >= 4.5, "contrast for " + theme + "/" + state);
            Check(sample.InstallActionBackground == ThemePalette.Update, "update action is green");
            Check((sample with { HasUpdate = false }).InstallActionBackground == "Transparent", "up-to-date addon is neutral");
            Check(warning.DependencyStatusColor == ThemePalette.Warning, "missing libraries are a warning");
            var listItem = new ListBoxItem { DataContext = warning, Content = "Fixture", IsSelected = true };
            Arrange(listItem);
            var row = (Border)listItem.Template.FindName("Row", listItem);
            Check(ColorOf(row.BorderBrush) == ColorOf(app.Resources["State.Accent"]), "selected warning row retains accent border");
            Check(row.CornerRadius == new CornerRadius(0), "square row");
            foreach (var templateName in new[] { "AddonListTemplate" })
            {
                var template = (DataTemplate)window.Resources[templateName];
                var presenter = new ContentPresenter { Content = sample, ContentTemplate = template };
                Arrange(presenter);
                Check(Visuals(presenter).OfType<Button>().Count(button => Equals(button.Tag, "AddonInstallAction")) == 1, "one action button in " + templateName);
                if (templateName == "AddonListTemplate")
                {
                    var comparison = (TextBlock)template.FindName("RowVersionComparison", presenter);
                    Check(comparison.Visibility == Visibility.Visible && comparison.Inlines.OfType<System.Windows.Documents.Run>().Last().Text == "2", "list retains version comparison");
                    Check(((TextBlock)template.FindName("RowCategories", presenter)).Text == sample.CategoryText, "category below name");
                    Check(((TextBlock)template.FindName("RowDownloads", presenter)).Text == sample.DownloadsText, "download count displayed");
                    var categoryIcon=(CategoryThumbnail)template.FindName("RowPreview",presenter);
                    Check(Visuals(categoryIcon).OfType<Image>().Single().Source is not null,"list icon comes from local resources");
                    var title = (TextBlock)template.FindName("RowTitle", presenter);
                    Check(title.Inlines.OfType<System.Windows.Documents.Run>().Last().Text.Contains("Tester"), "author beside name");
                }
            }
            var action = new Button { Tag = "AddonInstallAction", DataContext = sample, Content = "Update" };
            Arrange(action);
            action.DataContext = sample with { HasUpdate = false };
            action.UpdateLayout();
            Check(action.Visibility == Visibility.Collapsed, "installed status replaces inactive action");
            var check = new CheckBox { Content = "Select", IsChecked = true };
            Arrange(check);
            var box = (Border)check.Template.FindName("Box", check);
            Check(ColorOf(box.Background) == ColorOf(app.Resources["State.Accent"]) && box.CornerRadius == new CornerRadius(0), "square accent checkbox");
            Invoke(window, "UpdateLaunchButton");
            var launch = (Button)window.FindName("LaunchButton");
            var launchLabel = (TextBlock)window.FindName("LaunchLabel");
            Check(launchLabel.Text == UiText.Get("Запустить ESO") && launch.IsEnabled, "clear launch label");
            Check(ColorOf(((LucideIcon)window.FindName("LaunchIcon")).Foreground) == ColorOf(app.Resources["State.Update"]), "green launch icon only");
            typeof(MainWindow).GetField("launching", Private)!.SetValue(window, true);
            Invoke(window, "UpdateLaunchButton");
            Check(!launch.IsEnabled && launchLabel.Text == UiText.Get("Запускаем…"), "launch in-progress state without starting a process");
            typeof(MainWindow).GetField("launching", Private)!.SetValue(window, false);
            Invoke(window, "UpdateLaunchButton");
            var progress = (ProgressBar)window.FindName("OperationProgress");
            Check(ColorOf(progress.Foreground) == ColorOf(app.Resources["State.Accent"]), "progress uses theme accent");
            Invoke(window, "SetStatus", "Fixture error", true, false);
            var status = (TextBox)window.FindName("StatusLabel");
            Check(status.IsReadOnly && ColorOf(status.Foreground) == ColorOf(app.Resources["State.Error"]), "copyable pink error");
            Invoke(window, "SetStatus", "Fixture warning", false, true);
            Check(ColorOf(status.Foreground) == ColorOf(app.Resources["State.Warning"]), "yellow status warning");
            Invoke(window, "SetStatus", "Fixture normal", false, false);
            Check(ColorOf(status.Foreground) == ColorOf(app.Resources["Muted"]), "normal status resets color");
            typeof(MainWindow).GetField("folderChangeVersion", Private)!.SetValue(window, 2L);
            typeof(MainWindow).GetField("scannedFolderVersion", Private)!.SetValue(window, 0L);
            Invoke(window, "UpdateFolderChangeButton");
            foreach (var name in new[] { "CheckUpdatesButton", "ReloadCatalogButton" })
            {
                var button = (Button)window.FindName(name);
                Check(ColorOf(button.Background) == ColorOf(app.Resources["State.Warning"]), "whole warning button in both tabs");
                Arrange(button);
                Check(ColorOf(((Border)button.Template.FindName("ClassicOutline", button)).BorderBrush) == ColorOf(app.Resources["State.Warning"]), "button outline uses state color");
                Check(Visuals(button).OfType<TextBlock>().All(label => ColorOf(label.Foreground) == ColorOf(app.Resources["State.OnState"])), "button text uses contrast foreground");
            }
            typeof(MainWindow).GetField("scannedFolderVersion", Private)!.SetValue(window, 2L);
            Invoke(window, "UpdateFolderChangeButton");
            Check(((Button)window.FindName("CheckUpdatesButton")).ReadLocalValue(Control.BackgroundProperty) == DependencyProperty.UnsetValue, "fresh scan clears highlight");
            var releaseType = typeof(SettingsStore).Assembly.GetType("Boolka.ApplicationRelease")!;
            var release = Activator.CreateInstance(releaseType, new Version(99, 0, 0), new Uri("https://example.com/fixture.zip"), 1L, new string('0',64));
            typeof(MainWindow).GetField("applicationRelease", Private)!.SetValue(window, release);
            Invoke(window, "UpdateApplicationButton");
            var appUpdate = (Button)window.FindName("AppUpdateButton");
            Check(ColorOf(appUpdate.Background) == ColorOf(app.Resources["State.Update"]), "whole application-update button is green");
            Arrange(appUpdate);
            Check(ColorOf(((TextBlock)window.FindName("AppUpdateLabel")).Foreground) == ColorOf(app.Resources["State.OnState"]), "update label inherits contrast foreground");
            typeof(MainWindow).GetField("applicationRelease", Private)!.SetValue(window, null);
            Invoke(window, "UpdateApplicationButton");
            Check(!appUpdate.IsEnabled && appUpdate.ReadLocalValue(Control.BackgroundProperty) == DependencyProperty.UnsetValue, "no update clears highlight");
            var dialog = new OperationConfirmationWindow(window, "Fixture", "Fixture", destructive: true);
            var confirm = Logical(dialog).OfType<Button>().Single(x => Equals(x.Content, UiText.Get("Удалить")));
            Check(ColorOf(confirm.Background) == ColorOf(app.Resources["State.Danger"]), "destructive confirmation is red");
            Check(dialog.Width == 560 && dialog.Height == 340, "removal dialog is compact");
            var removalScroll = Logical(dialog).OfType<ScrollViewer>().Single();
            ((TextBlock)removalScroll.Content).Text = string.Join("\n", Enumerable.Range(1, 80).Select(i => $"C:\\Fixture\\AddOns\\Addon{i}"));
            var removalContent = (FrameworkElement)dialog.Content;
            removalContent.Measure(new Size(560, 340));
            removalContent.Arrange(new Rect(0, 0, 560, 340));
            removalContent.UpdateLayout();
            Check(removalScroll.ScrollableHeight > 0, "multiple removals scroll vertically");
            removalScroll.ScrollToBottom();
            removalContent.UpdateLayout();
            Check(removalScroll.VerticalOffset > 0, "last removal entry reachable");
            Check(!window.IsVisible && !dialog.IsVisible, "no windows shown");
            Invoke(window, "BeginOperation");
            Check(progress.IsIndeterminate, "unknown progress animates");
            Invoke(window, "DownloadProgressChanged", 1, 1, "Fixture", 50L, (long?)100L!);
            Check(!progress.IsIndeterminate && progress.Value == 50 && status.Text.Contains("50%"), "known progress includes percentage");
            Arrange(progress);
            Check(Visuals(progress).Any(node => node is Border border && border.Background is SolidColorBrush fill && fill.Color == ColorOf(app.Resources["State.Accent"])
                || node is System.Windows.Shapes.Shape shape && shape.Fill is SolidColorBrush color && color.Color == ColorOf(app.Resources["State.Accent"])), "rendered progress fill uses accent");
            Invoke(window, "EndOperation");
            Check(progress.Visibility == Visibility.Collapsed, "finished progress is hidden");
            ((ListBox)window.FindName("AddonList")).ItemsSource = new[] { sample, warning, sample with { Name = "Installed addon", HasUpdate = false } };
            Visuals((ListBox)window.FindName("AddonList")).OfType<ScrollViewer>().FirstOrDefault()?.ScrollToHome();
            ((FrameworkElement)window.FindName("EmptyState")).Visibility = Visibility.Collapsed;
            var content = (FrameworkElement)window.Content;
            content.Measure(new Size(1280,840)); content.Arrange(new Rect(0,0,1280,840)); content.UpdateLayout();
            Check(((Border)window.FindName("FolderCard")).ActualHeight <= 60, "folder card is compact");
            var folderLabel = (TextBlock)window.FindName("FolderLabel");
            Check(folderLabel.TextWrapping == TextWrapping.Wrap && folderLabel.TextTrimming == TextTrimming.None, "full folder path retains wrapping");
            var firstRow = ((ListBox)window.FindName("AddonList")).ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
            Check(firstRow is not null && firstRow.ActualHeight > 0 && firstRow.ActualHeight <= 64, "compact ordinary row height: " + firstRow?.ActualHeight);
            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(1280,840,96,96,PixelFormats.Pbgra32);
            bitmap.Render(content);
            var png = new System.Windows.Media.Imaging.PngBitmapEncoder(); png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            using (var file = File.Create(Path.Combine(fixture, theme + ".png"))) png.Save(file);
            Console.WriteLine("PREVIEW " + Path.Combine(fixture, theme + ".png"));
            Check(!window.Resources.Contains("AddonTilesTemplate") && window.FindName("CardsButton") is null && window.FindName("CompactButton") is null, "only list mode remains");
            var list = (ListBox)window.FindName("AddonList");
            list.ItemsSource = Enumerable.Range(0,200).Select(index => sample with {Name="Fixture "+index}).ToArray();
            content.Measure(new Size(1280,840));content.Arrange(new Rect(0,0,1280,840));content.UpdateLayout();
            Check(Visuals(list).OfType<VirtualizingStackPanel>().Any(), "list remains virtualized");
            Check(Visuals(list).OfType<ListBoxItem>().Count()<60, "only visible list containers are realized");
            var scroll=Visuals(list).OfType<ScrollViewer>().First();scroll.ScrollToBottom();content.UpdateLayout();
            Check(list.ItemContainerGenerator.ContainerFromIndex(199) is not null, "last list item reachable");
            Console.WriteLine("PASS " + theme);
        }
        Console.WriteLine("PASS all UI state checks; no network, game files, clipboard or visible windows");
    }
    static void Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private)!.Invoke(target, args);
    static void Arrange(FrameworkElement element) { element.ApplyTemplate(); element.Measure(new Size(350,100)); element.Arrange(new Rect(0,0,350,100)); element.UpdateLayout(); }
    static Color ColorOf(object brush) => ((SolidColorBrush)brush).Color;
    static double Luminance(Color c) { double Linear(byte b) { var v=b/255.0; return v<=0.04045?v/12.92:Math.Pow((v+0.055)/1.055,2.4); } return 0.2126*Linear(c.R)+0.7152*Linear(c.G)+0.0722*Linear(c.B); }
    static double Contrast(Color a, Color b) { var x=Luminance(a);var y=Luminance(b);return (Math.Max(x,y)+0.05)/(Math.Min(x,y)+0.05); }
    static void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
    static IEnumerable<DependencyObject> Visuals(DependencyObject node) { yield return node; for (var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++) foreach(var child in Visuals(VisualTreeHelper.GetChild(node,i))) yield return child; }
    static IEnumerable<DependencyObject> Logical(DependencyObject node) { yield return node; foreach(var child in node is ContentPresenter presenter && presenter.Content is DependencyObject content ? new[]{content} : LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>()) foreach(var nested in Logical(child))yield return nested; }
}
