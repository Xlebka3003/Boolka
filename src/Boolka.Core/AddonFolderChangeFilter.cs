using System.IO;

namespace Boolka;

public static class AddonFolderChangeFilter
{
    public static bool IsRelevant(string root, string path, WatcherChangeTypes change)
    {
        var relative = Path.GetRelativePath(root, path);
        var parts = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts[0] is "." or ".." || Path.IsPathRooted(relative) ||
            parts.Any(part => part.Equals("SavedVariables", StringComparison.OrdinalIgnoreCase))) return false;

        // Изменение содержимого папки не означает изменение состава аддонов.
        // События имён верхнего уровня сохраняем и для уже удалённых папок.
        if (parts.Length == 1) return change != WatcherChangeTypes.Changed;

        // Сканер распознаёт манифест по имени родительской папки, в том числе у вложенных библиотек.
        var extension = Path.GetExtension(parts[^1]);
        return (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) || extension.Equals(".addon", StringComparison.OrdinalIgnoreCase)) &&
            Path.GetFileNameWithoutExtension(parts[^1]).Equals(parts[^2], StringComparison.OrdinalIgnoreCase);
    }
}
