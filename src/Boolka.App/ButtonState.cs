using System.Windows;
using System.Windows.Controls;

namespace Boolka;

internal static class ButtonState
{
    internal static void Select(Button button, bool selected)
    {
        button.SetResourceReference(Control.BackgroundProperty, selected ? "State.SelectionBackground" : "Theme.10131B");
        button.SetResourceReference(Control.BorderBrushProperty, selected ? "State.Accent" : "Theme.353C4E");
        button.SetResourceReference(Control.ForegroundProperty, "Theme.E9EDF5");
    }

    internal static void Apply(Button button, string? state)
    {
        if (state is null)
        {
            button.ClearValue(Control.BackgroundProperty);
            button.ClearValue(Control.BorderBrushProperty);
            button.ClearValue(Control.ForegroundProperty);
            button.ClearValue(Control.FontWeightProperty);
            return;
        }
        button.SetResourceReference(Control.BackgroundProperty, "State." + state);
        button.SetResourceReference(Control.BorderBrushProperty, "State." + state);
        button.SetResourceReference(Control.ForegroundProperty, "State.OnState");
        button.FontWeight = FontWeights.SemiBold;
    }
}
