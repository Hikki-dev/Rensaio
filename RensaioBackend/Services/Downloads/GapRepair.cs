namespace RensaioBackend.Services.Downloads;

/// <summary>
/// Finds holes in a series: chapters between the lowest and the highest one you have that nothing ever downloaded
/// (a failed download, a source that was down for a while, a chapter a site added late). Without this a hole stays
/// forever, because the "continue after" mark has moved past it.
/// </summary>
public static class GapRepair
{
    public const int MaxPerRun = 25;   // a long hole is filled bit by bit, so sites are not hammered

    /// <param name="listed">chapter numbers this source offers</param>
    /// <param name="held">numbers any source downloaded (a deliberately removed one counts as held)</param>
    /// <param name="onDisk">numbers found in the series folder, whoever put them there</param>
    /// <param name="continueAfter">the mark the normal download path already starts after</param>
    public static List<decimal> Pick(IEnumerable<decimal> listed, IEnumerable<decimal> held, IEnumerable<decimal> onDisk, decimal? continueAfter, int max = MaxPerRun)
    {
        if (continueAfter == null)
            return [];                       // nothing is cut off, so the normal path already asks for everything
        HashSet<decimal> have = [.. held, .. onDisk];
        if (have.Count == 0)
            return [];
        decimal floor = have.Min(), ceiling = have.Max();      // holes are only between what you own, never before your first chapter
        return listed.Distinct()
            .Where(n => n > floor && n < ceiling && n <= continueAfter && !have.Contains(n))
            .Order().Take(max).ToList();
    }
}
