namespace Boolka;

public static class ThemePalette
{
    public static bool IsLight { get; set; }
    public static bool IsDracula { get; set; }
    // Свойство задаёт только квадратную форму, независимо от цвета.
    public static bool IsClassic { get; set; }

    public static string Accent => IsDracula ? "#BD93F9" : IsLight ? "#155DA8" : "#82B7FF";
    public static string Update => IsLight ? "#187047" : "#50FA7B";
    public static string Warning => IsLight ? "#805900" : "#F1FA8C";
    public static string Error => IsLight ? "#A81D6B" : "#FF79C6";
    public static string Danger => IsLight ? "#B42318" : "#FF6B6B";
    public static string OnState => IsLight ? "#FFFFFF" : "#202124";
    public static string SelectionBackground => IsDracula ? "#353148" : IsLight ? "#E3EEF9" : "#26364A";

    public static string Map(string color) => color.ToUpperInvariant() switch
    {
        "#B7A4FF" or "#A795EF" or "#A995EC" or "#B8A3F2" or "#C8B7FF" or "#C7B9FF" or "#B9A9F4" => Accent,
        _ => IsDracula ? Dracula(color) : IsLight ? Warm(color) : Graphite(color)
    };

    private static string Dracula(string color) => color.ToUpperInvariant() switch
    {
        "#10131B" or "#10131BFF" => "#282A36",
        "#151923" => "#21222C",
        "#191E29" or "#1B202C" or "#1B2030" or "#2B273D" => "#303240",
        "#222735" or "#252B39" or "#242A38" or "#29253B" or "#26233A" or "#353044" => "#44475A",
        "#252A36" or "#262D3C" => "#565970",
        "#353C4E" or "#454D62" => "#6272A4",
        "#E9EDF5" or "#E0E5EF" => "#F8F8F2",
        "#9AA3B5" or "#737D90" => "#C4C7DE",
        "#14121D" => "#282A36",
        "#A995EC" or "#B8A3F2" => "#BD93F9",
        "#645B83" => "#565970",
        "#C8B7FF" or "#C7B9FF" or "#B9A9F4" => "#8BE9FD",
        "#B7A4FF" or "#A795EF" => "#FF79C6",
        "#7DDDC0" or "#94D6AE" => "#50FA7B",
        "#F28B93" or "#D66F7B" => "#FF8B8B",
        "#F1BDA9" => "#FFB86C",
        "#E8C17A" => "#F1FA8C",
        "#F010131B" => "#F0282A36",
        "#8098A4BD" => "#806272A4",
        "#B0B7A4FF" => "#B0BD93F9",
        "#E0B7A4FF" => "#E0BD93F9",
        _ => color
    };

    private static string Graphite(string color) => color.ToUpperInvariant() switch
    {
        "#10131B" or "#10131BFF" => "#202124",
        "#151923" => "#242529",
        "#191E29" or "#1B202C" => "#292B30",
        "#1B2030" => "#2D2F35",
        "#222735" or "#252B39" or "#242A38" => "#35373F",
        "#29253B" or "#26233A" or "#353044" => "#35373F",
        "#2B273D" => "#292D34",
        "#252A36" or "#262D3C" => "#4A4C55",
        "#353C4E" or "#454D62" => "#777986",
        "#E9EDF5" or "#E0E5EF" => "#E8EAED",
        "#9AA3B5" => "#B5B8C2",
        "#737D90" => "#AAADBA",
        "#14121D" => "#14121D",
        "#A995EC" or "#B8A3F2" => "#C4B2EB",
        "#645B83" => "#635975",
        "#F010131B" => "#F0202124",
        _ => color
    };

    private static string Warm(string color) => color.ToUpperInvariant() switch
    {
        "#10131B" or "#10131BFF" => "#F5F6F8",
        "#151923" => "#E9ECF0",
        "#191E29" or "#1B202C" => "#FFFFFF",
        "#1B2030" => "#FFFFFF",
        "#222735" or "#252B39" or "#242A38" => "#E6E9ED",
        "#29253B" or "#26233A" or "#353044" => "#E3EEF9",
        "#2B273D" => "#EEF1F5",
        "#252A36" or "#262D3C" => "#BBC3CD",
        "#353C4E" or "#454D62" => "#737E8D",
        "#E9EDF5" or "#E0E5EF" => "#202833",
        "#9AA3B5" => "#525E6D",
        "#737D90" => "#525E6D",
        "#14121D" => "#292333",
        "#A995EC" or "#B8A3F2" => "#D7C5ED",
        "#645B83" => "#D4CADC",
        "#C8B7FF" or "#C7B9FF" or "#B9A9F4" or "#B7A4FF" or "#A795EF" => "#69468F",
        "#7DDDC0" or "#94D6AE" => "#096A4A",
        "#F28B93" or "#D66F7B" => "#AB2740",
        "#F1BDA9" => "#93431F",
        "#E8C17A" => "#7D560D",
        "#F010131B" => "#F0F4F1EB",
        "#8098A4BD" => "#80655E56",
        "#B0B7A4FF" => "#B069468F",
        "#E0B7A4FF" => "#E069468F",
        _ => color
    };
}
