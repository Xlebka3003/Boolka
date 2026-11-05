using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Boolka;

/// <summary>Локальная иконка категории без загрузки скриншотов и переключаемых режимов.</summary>
public sealed class CategoryThumbnail : Border
{
    public static readonly DependencyProperty CategoryIdProperty = DependencyProperty.Register(nameof(CategoryId), typeof(long), typeof(CategoryThumbnail),
        new PropertyMetadata(0L, (target, _) => ((CategoryThumbnail)target).UpdateIcon()));
    public long CategoryId { get => (long)GetValue(CategoryIdProperty); set => SetValue(CategoryIdProperty, value); }
    private readonly Image image = new() { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly };

    public CategoryThumbnail()
    {
        ClipToBounds = true;
        SetResourceReference(BackgroundProperty, "Theme.2B273D");
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        Child = image;
        UiResources.Register("Иконка категории ESO");
        SetResourceReference(System.Windows.Automation.AutomationProperties.HelpTextProperty, UiText.ResourceKey("Иконка категории ESO"));
        UpdateIcon();
    }

    private void UpdateIcon() => image.Source = CategoryIcons.Get(CategoryId);
}
