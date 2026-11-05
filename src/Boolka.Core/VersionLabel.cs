using System.Text.RegularExpressions;

namespace Boolka;

public static class VersionLabel
{
    public static string Compact(Addon addon)
    {
        var label = Format(addon);
        var human = addon.InstalledReleaseVersion ?? addon.ManifestVersion ?? addon.Version;
        if (string.IsNullOrWhiteSpace(human) || human == "Не указана") return label;
        var prefix = UiText.Format($"Версия {string.Empty}");
        return label.StartsWith(prefix, StringComparison.Ordinal) ? label[prefix.Length..] : label;
    }

    public static string Format(Addon addon)
    {
        var human = addon.InstalledReleaseVersion ?? addon.ManifestVersion ?? addon.Version;
        if (string.IsNullOrWhiteSpace(human) || human == "Не указана")
            return string.IsNullOrWhiteSpace(addon.BuildVersion) ? UiText.Get("Версия не указана") : UiText.Format($"Сборка {addon.BuildVersion}");
        var build = Regex.Match(human, @"^(?:сборка|build|билд)\s*:?\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (build.Success) return UiText.Format($"Сборка {build.Groups[1].Value}");
        human = Regex.Replace(human.Trim(), @"^(?:version|версия)\s*:?\s*", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return UiText.Format($"Версия {human}");
    }
}
