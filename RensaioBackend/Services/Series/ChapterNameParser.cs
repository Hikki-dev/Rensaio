using System.Globalization;
using System.Text.RegularExpressions;

namespace RensaioBackend.Services.Series;

/// <summary>
/// Reads the chapter number out of an archive file name. Understands "Ch.230", "Chapter 230", "c230",
/// "Title 230.cbz" and "Vol.23 Ch.229"; volume numbers and years are never read as chapters, and a
/// "c" inside a word ("Disc 2") is not a chapter marker.
/// </summary>
public static class ChapterNameParser
{
    private static readonly string[] Extensions = [".cbz", ".cbr", ".zip", ".pdf", ".epub"];
    private static readonly Regex Noise = new(@"\(\d{4}\)|\bvol(?:ume)?s?[\s._-]*\d+(?:\.\d+)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // "Ch.12", "Chapter 12", "c12" and also a file that holds several chapters: "Ch.12-14"
    private static readonly Regex Marked = new(@"(?<![A-Za-z])(?:ch(?:apters?)?|c)[\s._-]*(\d+(?:\.\d+)?)(?:\s*[-\u2013~]\s*(\d+(?:\.\d+)?))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // "Title 231" at the end of the name, also "Title 12-14"
    private static readonly Regex Trailing = new(@"[\s_-](\d{1,4}(?:\.\d+)?)(?:\s*[-\u2013~]\s*(\d{1,4}(?:\.\d+)?))?(?:\s*v\d)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private const int LongestRange = 30;   // "Ch.1-500" is not a real volume of chapters

    /// <summary>All chapter numbers a file name stands for: one, or the whole range of "Ch.12-14".</summary>
    public static List<decimal> ChaptersOf(string fileName)
    {
        string name = Noise.Replace(Path.GetFileNameWithoutExtension(fileName), " ");
        foreach (Regex rx in new[] { Marked, Trailing })
        {
            Match m = rx.Match(name);
            if (!m.Success || !Parse(m.Groups[1].Value, out decimal first))
                continue;
            if (m.Groups[2].Success && Parse(m.Groups[2].Value, out decimal last) && last > first)
            {
                if (first == decimal.Truncate(first) && last == decimal.Truncate(last) && last - first <= LongestRange)
                    return Enumerable.Range((int)first, (int)(last - first) + 1).Select(n => (decimal)n).ToList();
                return last - first <= LongestRange ? [first, last] : [first];
            }
            return [first];
        }
        return [];
    }

    public static decimal? ChapterOf(string fileName) => ChaptersOf(fileName) is { Count: > 0 } l ? l[0] : null;

    private static bool Parse(string text, out decimal n) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out n);

    /// <summary>The chapters found in a folder, each with the file that holds it (the first file wins).</summary>
    public static Dictionary<decimal, string> ScanFolder(string folder)
    {
        Dictionary<decimal, string> found = [];
        if (!Directory.Exists(folder))
            return found;
        foreach (string f in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly).Order(StringComparer.OrdinalIgnoreCase))
        {
            if (!Extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                continue;
            foreach (decimal n in ChaptersOf(f))
                found.TryAdd(n, Path.GetFileName(f));
        }
        return found;
    }
}
