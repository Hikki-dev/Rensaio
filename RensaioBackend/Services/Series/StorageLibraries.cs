namespace RensaioBackend.Services.Series;

/// <summary>
/// Extra library folders ("Manga 18+", "Comics" ...) that sit NEXT TO the main storage folder,
/// inside the same mount, so moving a series between libraries is a rename, never a copy.
/// A series in the main folder keeps a relative StoragePath; a series in an extra library keeps
/// the absolute path of its folder. Nothing outside the main folder and the listed libraries is
/// ever accepted.
/// </summary>
public static class StorageLibraries
{
    public const int MaxLibraries = 20;
    private static readonly char[] Bad = ['/', '\\', ':', '*', '?', '"', '<', '>', '|', '\0'];

    /// <summary>Checks the typed names and returns them clean and distinct (case-insensitive).</summary>
    public static string[] CleanNames(string storageFolder, IEnumerable<string>? names)
    {
        string main = Path.GetFileName(storageFolder.TrimEnd('/', '\\'));
        var result = new List<string>();
        foreach (string raw in names ?? [])
        {
            string name = (raw ?? string.Empty).Trim();
            if (name.Length == 0) continue;
            if (name.IndexOfAny(Bad) >= 0 || name == "." || name == ".." || name.StartsWith('.') || name.EndsWith('.'))
                throw new ArgumentException($"'{name}' is not a usable folder name (no slashes, no leading or trailing dots).");
            if (name.Length > 100)
                throw new ArgumentException("A library name must be 100 characters or fewer.");
            if (string.Equals(name, main, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"'{name}' is already the main library.");
            if (result.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"'{name}' is listed twice.");
            result.Add(name);
        }
        if (result.Count > MaxLibraries)
            throw new ArgumentException($"At most {MaxLibraries} extra libraries.");
        return result.ToArray();
    }

    /// <summary>Absolute folders of the extra libraries.</summary>
    public static string[] Roots(string storageFolder, IEnumerable<string>? names)
    {
        string parent = Path.GetDirectoryName(storageFolder.TrimEnd('/', '\\')) ?? string.Empty;
        return (names ?? []).Select(n => Path.Combine(parent, n)).ToArray();
    }

    /// <summary>
    /// Turns a path asked for by the UI or an API call into the form kept in the database:
    /// relative for the main folder, absolute for an extra library.
    /// </summary>
    /// <exception cref="ArgumentException">Empty path, traversal, or a folder that is not in any library.</exception>
    public static string ToStored(string storageFolder, IEnumerable<string>? names, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Series storage path must not be empty.");
        string raw = path.Trim().Replace('\\', '/');
        if (raw.Contains('\0'))
            throw new ArgumentException("Series storage path has an invalid character.");

        bool rooted = raw.StartsWith('/') || (raw.Length >= 2 && raw[1] == ':');
        string[] segments = raw.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(s => s == "." || s == ".."))
            throw new ArgumentException("Series storage path must not contain '.' or '..' segments.");
        if (!rooted)
            return string.Join(Path.DirectorySeparatorChar, segments); // inside the main folder, as before

        string main = storageFolder.Replace('\\', '/').TrimEnd('/');
        foreach (string root in new[] { main }.Concat(Roots(storageFolder, names).Select(r => r.Replace('\\', '/').TrimEnd('/'))))
        {
            if (!raw.StartsWith(root + "/", StringComparison.Ordinal)) continue;
            string rest = string.Join(Path.DirectorySeparatorChar, raw[(root.Length + 1)..].Split('/', StringSplitOptions.RemoveEmptyEntries));
            if (rest.Length == 0)
                throw new ArgumentException("Pick a series folder inside the library, not the library itself.");
            return root == main ? rest : root.Replace('/', Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar + rest;
        }
        throw new ArgumentException("That folder is not in the main library or in one of the extra libraries.");
    }

    /// <summary>The library a stored path is in: null for the main one, else the extra library's name.</summary>
    public static string? LibraryOf(string storageFolder, IEnumerable<string>? names, string storedPath)
    {
        string p = storedPath.Replace('\\', '/');
        foreach (string name in names ?? [])
            if (p.StartsWith(Roots(storageFolder, [name])[0].Replace('\\', '/').TrimEnd('/') + "/", StringComparison.Ordinal))
                return name;
        return null;
    }
}
