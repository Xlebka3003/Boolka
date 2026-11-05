using System.Windows;
using System.Windows.Controls;

namespace Boolka;

public sealed class OperationConfirmationWindow : Window
{
    public OperationConfirmationWindow(Window owner, string title, string summary, bool destructive = false)
    {
        if (owner.IsVisible) Owner = owner;
        Title = title;
        Width = destructive ? 560 : 680;
        Height = destructive ? 340 : Math.Clamp(owner.ActualHeight * 0.8, 400, 720);
        MinWidth = 440;
        MinHeight = destructive ? 280 : 320;
        Background = owner.Background;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var grid = new Grid { Margin = new Thickness(24) };
        grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new());
        grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
        grid.Children.Add(new TextBlock { Text = title, FontSize = 24, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 18) });
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = new TextBlock { Text = summary, TextWrapping = TextWrapping.Wrap, FontSize = 14, LineHeight = 22, Margin = new Thickness(0, 0, 12, 0) } };
        Grid.SetRow(scroll, 1);
        grid.Children.Add(scroll);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
        // Enter по умолчанию отменяет операцию; подтверждение требует намеренного выбора.
        actions.Children.Add(new Button { Content = UiText.Get("Отмена"), IsCancel = true, IsDefault = true, Margin = new Thickness(0, 0, 10, 0) });
        var confirm = new Button { Content = UiText.Get(destructive ? "Удалить" : "Продолжить") };
        if (destructive) ButtonState.Apply(confirm, "Danger");
        confirm.Click += (_, _) => DialogResult = true;
        actions.Children.Add(confirm);
        Grid.SetRow(actions, 2);
        grid.Children.Add(actions);
        Content = grid;
        DialogChrome.Apply(this);
    }
}
