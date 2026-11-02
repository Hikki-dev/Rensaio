using System.Globalization;
using System.Text.RegularExpressions;
using RensaioBackend.Models.Database;

namespace RensaioBackend.Services.Series;

/// <summary>
/// Finds the chapters that already sit in a series folder, so adding a series never downloads them again
/// (and chapters missing in the middle are still fetched). Understands "Ch.230", "Chapter 230", "c230",
/// "Title 230.cbz" and "Vol.23 Ch.229"; volume numbers and years are never read as chapters.
/// </summary>
public static class DiskChapterScanner
{
    private static readonly string[] Extensions = [".cbz", ".cbr", ".zip", ".pdf", ".epub"];
    private static readonly Regex Noise = new(@"\(\d{4}\)|\bvol(?:ume)?[\s._-]*\d+(?:\.\d+)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex[] Patterns =
    [
        new(@"(?:ch(?:apter)?|c)[\s._-]*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"[\s_-](\d{1,4}(?:\.\d+)?)(?:\s*v\d)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    public static decimal? ChapterOf(string fileName)
    {
        string name = Noise.Replace(Path.GetFileNameWithoutExtension(fileName), " ");
        foreach (Regex rx in Patterns)
        {
            Match m = rx.Match(name);
            if (m.Success && decimal.TryParse(m.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal n))
                return n;
        }
        return null;
    }

    public static Dictionary<decimal, string> Scan(string folder)
    {
        Dictionary<decimal, string> found = [];
        if (!Directory.Exists(folder))
            return found;
        foreach (string f in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly))
        {
            if (!Extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                continue;
            decimal? n = ChapterOf(f);
            if (n.HasValue && !found.ContainsKey(n.Value))
                found[n.Value] = Path.GetFileName(f);
        }
        return found;
    }

    /// <summary>
    /// Marks the chapters that are already on disk as downloaded, on one provider (the permanent one if there is
    /// one). The download logic then skips them everywhere and continues after the highest. Returns how many.
    /// </summary>
    public static int MarkExisting(List<SeriesProviderEntity> providers, string folder, ILogger logger)
    {
        Dictionary<decimal, string> disk = Scan(folder);
        SeriesProviderEntity? target = providers.Where(p => !p.IsDisabled && !p.IsUninstalled && p.Chapters.Count > 0)
            .OrderByDescending(p => p.IsStorage).ThenByDescending(p => p.Chapters.Count).FirstOrDefault();
        if (disk.Count == 0 || target == null)
            return 0;
        // a stray number (a year read as a chapter) must not skip a whole series
        decimal top = providers.SelectMany(p => p.Chapters).Max(c => c.Number) ?? 0;
        int marked = 0;
        foreach (Models.Chapter c in target.Chapters)
        {
            if (c.Number == null || !string.IsNullOrEmpty(c.Filename) || c.Number > top + 10)
                continue;
            if (disk.TryGetValue(c.Number.Value, out string? file))
            {
                c.Filename = file;
                c.DownloadDate = File.GetLastWriteTimeUtc(Path.Combine(folder, file));
                c.ShouldDownload = false;
                c.IsDeleted = false;
                marked++;
            }
        }
        logger.LogInformation("{count} of {total} chapters found on disk in {folder}; they will not be downloaded again.", marked, disk.Count, folder);
        return marked;
    }
}
