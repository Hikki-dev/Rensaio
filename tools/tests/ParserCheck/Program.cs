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
Console.WriteLine(bad == 0 ? "parser ok" : $"{bad} failed"); return bad;
