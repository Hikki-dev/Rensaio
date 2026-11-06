namespace RensaioBackend.Services.Helpers;

/// <summary>Small rules that turn what a source tells us into ComicInfo.xml values Komga understands.</summary>
public static class ComicMetadata
{
    private static readonly string[] Adult = ["adult", "hentai", "smut", "erotica", "pornographic", "pornography", "mature content"];
    private static readonly string[] Mature = ["mature", "ecchi", "gore", "psychological", "sexual violence"];

    /// <summary>Komga's age rating text for a list of genres: "" when nothing points to an age limit.</summary>
    public static string AgeRating(IEnumerable<string>? genres)
    {
        var g = (genres ?? []).Select(x => x.Trim().ToLowerInvariant()).ToList();
        if (g.Any(x => Adult.Contains(x))) return "Adults Only 18+";
        if (g.Any(x => Mature.Contains(x))) return "Mature 17+";
        return "";
    }

    /// <summary>The ComicInfo "Manga" flag: manga reads right to left, manhwa and manhua left to right.</summary>
    public static string MangaFlag(string? type, IEnumerable<string>? genres)
    {
        string t = (type ?? "").Trim().ToLowerInvariant();
        var g = (genres ?? []).Select(x => x.Trim().ToLowerInvariant()).ToList();
        if (t == "manga" || (t == "" && g.Contains("manga"))) return "YesAndRightToLeft";
        if (t is "manhwa" or "manhua" or "webtoon") return "Yes";
        return "";
    }

    /// <summary>Plain text for a summary: tags removed, spaces tidied, never longer than 4000 characters.</summary>
    public static string Summary(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        string s = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", " ");
        s = System.Net.WebUtility.HtmlDecode(s);
        s = System.Text.RegularExpressions.Regex.Replace(s, @"[ \t]+", " ").Trim();
        return s.Length > 4000 ? s[..4000] : s;
    }
}
