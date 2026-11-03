using RensaioBackend.Services.Downloads;
using RensaioBackend.Services.Series;
var cases = new (string name, decimal? want)[] {
    ("Chainsaw Man - Ch.230.cbz", 230), ("Chainsaw Man - Vol.23 Ch.229.cbz", 229), ("[MangaKatana][en] Chainsaw Man 231.cbz", 231),
    ("One Piece - Chapter 1000.cbz", 1000), ("Title Ch.12.5.cbz", 12.5m), ("Title c045.cbz", 45), ("Title - 7 v2.cbz", 7),
    ("Disc 2.cbz", 2), ("Disc Chapter 9.cbz", 9), ("Title (2020) Ch.3.cbz", 3), ("Title (2020).cbz", null), ("cover.jpg", null), ("Vol.5.cbz", null),
};
var many = new (string name, string want)[] {
    ("Chainsaw Man - Ch.12-14.cbz", "12,13,14"), ("Title Chapters 5-6.cbz", "5,6"), ("Title c7~9.cbz", "7,8,9"),
    ("Title 12-13.cbz", "12,13"), ("Title - 8.cbz", "8"), ("Title Ch.10.5-11.cbz", "10.5,11"), ("Title Ch.1-500.cbz", "1"),
    ("Title Ch.20-10.cbz", "20"), ("Title Vol.2 Ch.15.cbz", "15"), ("Title v3.cbz", ""), ("Title Ch.012.cbz", "12"), ("Title Ch.0.cbz", "0"),
    ("題名 第12話 Ch.12.cbz", "12"), ("Title Ch.12 [Group].cbz", "12"),
};
int bad = 0;
foreach (var (name, want) in many) { var got = string.Join(",", ChapterNameParser.ChaptersOf(name)); if (got != want) { Console.WriteLine($"FAIL {name}: got [{got}], want [{want}]"); bad++; } }
// a folder with real files: ranges, duplicates and noise
var dir = Path.Combine(Path.GetTempPath(), "parsercheck-" + Guid.NewGuid()); Directory.CreateDirectory(dir);
foreach (var f in new[] { "S - Ch.1.cbz", "S - Ch.2-3.cbz", "S - Ch.3.cbz", "S - cover.jpg", "S - Ch.4.txt", "S - Ch.5.CBR" }) File.WriteAllText(Path.Combine(dir, f), "");
var scan = ChapterNameParser.ScanFolder(dir);
if (string.Join(",", scan.Keys.OrderBy(k => k)) != "1,2,3,5") { Console.WriteLine("FAIL scan keys: " + string.Join(",", scan.Keys.OrderBy(k => k))); bad++; }
if (scan[3] != "S - Ch.2-3.cbz") { Console.WriteLine("FAIL first file should win for 3: " + scan[3]); bad++; }
if (ChapterNameParser.ScanFolder(Path.Combine(dir, "missing")).Count != 0) { Console.WriteLine("FAIL missing folder"); bad++; }
Directory.Delete(dir, true);
foreach (var (name, want) in cases) { var got = ChapterNameParser.ChapterOf(name); if (got != want) { Console.WriteLine($"FAIL {name}: got {got}, want {want}"); bad++; } }
// which listed chapters are real chapters
void Main_(decimal n, string? name, bool want, string label, bool frac = false, params decimal[] whole) { if (ChapterFilter.IsMainChapter(n, name, new HashSet<decimal>(whole), frac) != want) { Console.WriteLine($"FAIL filter {label}"); bad++; } }
Main_(5, "Chapter 5", true, "plain chapter"); Main_(5, null, true, "no name"); Main_(1197, "Chapter 1197: Title", true, "title");
Main_(10.5m, "Chapter 10.5", false, "half chapter next to its whole chapter", false, 10); Main_(10.5m, "Chapter 10.5", true, "half chapter allowed on request", true, 10);
Main_(1046.555m, "Chapter 1046.555", false, "nonsense fraction next to its chapter", false, 1046); Main_(1.1m, "Chapter 1.1", false, "re-upload part", false, 1);
Main_(24.1m, "Chapter 24.1", true, "part of a chapter that has no whole chapter", false, 23, 25); Main_(24.2m, "Chapter 24.2", true, "second part", false, 23, 25);
Main_(187, "Bounus.187.5", false, "bonus by name"); Main_(19, "Vol.19: Bonus: Reality Strikes", false, "volume bonus"); Main_(6, "Oneshot: Young Jump Special 6", false, "oneshot");
Main_(0, "Vol.tbd Extras", false, "extras"); Main_(4, "Vol.4 Entracte: Chambre", false, "volume extra");
Main_(12, "Chapter 12: Special Training", true, "a title that says special is still a chapter"); Main_(12, "Ch.12 - Bonus Round", true, "chapter word wins");
Main_(1, "Vol.1 Ch.1 - A Dog and a Chainsaw", true, "MangaDex style name"); Main_(230, "Vol.23 Chapter 230", true, "volume and chapter");
Main_(-1, "x", false, "negative"); Main_(0, "Prologue", true, "prologue is chapter 0");
// gap repair
static string Gaps(decimal[] listed, decimal[] held, decimal[] disk, decimal? after, int max = 25) => string.Join(",", GapRepair.Pick(listed, held, disk, after, max));
decimal[] R(int a, int b) => Enumerable.Range(a, b - a + 1).Select(n => (decimal)n).ToArray();
void Expect(string got, string want, string label) { if (got != want) { Console.WriteLine($"FAIL gap {label}: got [{got}], want [{want}]"); bad++; } }
Expect(Gaps(R(1, 20), [1, 2, 3, 7, 8], [], 8), "4,5,6", "hole between owned chapters");
Expect(Gaps(R(1, 20), [10, 11, 12], [], 12), "", "never fills before the first chapter you have");
Expect(Gaps(R(1, 20), [1, 2, 3, 7, 8], [4, 5], 8), "6", "chapters found on disk are not holes");
Expect(Gaps(R(1, 20), [1, 8], [], null), "", "no continue-after mark: normal path covers it");
Expect(Gaps(R(1, 20), [], [], 8), "", "nothing owned, nothing to repair");
Expect(Gaps(R(1, 200), [1, 150], [], 150, 5), "2,3,4,5,6", "a long hole is filled a few at a time");
Expect(Gaps([1, 2, 3.5m, 4, 5], [1, 5], [], 5), "2,3.5,4", "fractional chapters");
Expect(Gaps(R(1, 10), [1, 10], [], 5), "2,3,4,5", "only up to the continue-after mark, the rest is the normal path");
Expect(Gaps([2, 2, 3], [1, 4], [], 4), "2,3", "duplicate listings");
Console.WriteLine(bad == 0 ? "parser ok" : $"{bad} failed"); return bad;
