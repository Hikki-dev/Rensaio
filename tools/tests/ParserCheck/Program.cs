using RensaioBackend.Services.Series;
var cases = new (string name, decimal? want)[] {
    ("Chainsaw Man - Ch.230.cbz", 230), ("Chainsaw Man - Vol.23 Ch.229.cbz", 229), ("[MangaKatana][en] Chainsaw Man 231.cbz", 231),
    ("One Piece - Chapter 1000.cbz", 1000), ("Title Ch.12.5.cbz", 12.5m), ("Title c045.cbz", 45), ("Title - 7 v2.cbz", 7),
    ("Disc 2.cbz", 2), ("Disc Chapter 9.cbz", 9), ("Title (2020) Ch.3.cbz", 3), ("Title (2020).cbz", null), ("cover.jpg", null), ("Vol.5.cbz", null),
};
int bad = 0;
foreach (var (name, want) in cases) { var got = ChapterNameParser.ChapterOf(name); if (got != want) { Console.WriteLine($"FAIL {name}: got {got}, want {want}"); bad++; } }
Console.WriteLine(bad == 0 ? "parser ok" : $"{bad} failed"); return bad;
