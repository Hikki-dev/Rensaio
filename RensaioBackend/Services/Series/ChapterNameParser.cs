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
    private static readonly Regex Noise = new(@"\(\d{4}\)|\bvol(?:ume)?[\s._-]*\d+(?:\.\d+)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex[] Patterns =
    [
        new(@"(?<![A-Za-z])(?:ch(?:apter)?|c)[\s._-]*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
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

}
