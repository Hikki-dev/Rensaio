using System.Text.RegularExpressions;

namespace RensaioBackend.Services.Downloads;

/// <summary>
/// Decides which of the "chapters" a site lists are real chapters. Sites also list extras, bonus pages, one-shots,
/// volume covers and several uploads of the same chapter (10.5, 1046.555, "Vol.19: Bonus"). Those are not wanted by default.
/// </summary>
public static class ChapterFilter
{
    private static readonly Regex Regular = new(@"\b(chapter|chap|ch)\b\.?\s*\d", RegexOptions.IgnoreCase | RegexOptions.Compiled);   // "Chapter 5", "Ch.5", also "Vol.1 Ch.1 - Title"
    private static readonly Regex Extra = new(@"(\bvol(ume)?\b\.?\s*\w+|\bbonus\b|\bbounus\b|\bextras?\b|\bone[- ]?shot\b|\bomake\b|\bspecial edition\b|\bcolou?r (spread|pages?)\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Set RENSAIO_ALLOW_FRACTIONAL_CHAPTERS=true to also get chapters like 10.5.</summary>
    public static bool AllowFractions { get; set; } =
        string.Equals(Environment.GetEnvironmentVariable("RENSAIO_ALLOW_FRACTIONAL_CHAPTERS"), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// A listed chapter is wanted when it is a whole number, or a part of a chapter (24.1, 24.2) that has no whole chapter
    /// of its own in the list. A number with a fraction next to its whole chapter (10 and 10.5, 1046 and 1046.555) is an
    /// extra or a re-upload. Names such as "Vol.19: Bonus" or "Oneshot" are never wanted.
    /// </summary>
    /// <param name="wholeChaptersListed">the whole chapter numbers this site lists for the series</param>
    public static bool IsMainChapter(decimal number, string? name, ISet<decimal> wholeChaptersListed) =>
        IsMainChapter(number, name, wholeChaptersListed, AllowFractions);

    public static bool IsMainChapter(decimal number, string? name, ISet<decimal> wholeChaptersListed, bool allowFractions)
    {
        if (number < 0)
            return false;
        if (!allowFractions && number != decimal.Truncate(number) && wholeChaptersListed.Contains(decimal.Truncate(number)))
            return false;
        if (!string.IsNullOrWhiteSpace(name) && !Regular.IsMatch(name) && Extra.IsMatch(name))
            return false;
        return true;
    }
}
