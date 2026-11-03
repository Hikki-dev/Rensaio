# What this fork changes

Based on maxpiva/Rensaio at 0b647ef (itself the maintained successor of the archived oae/kaizoku). Same GPL-3.0 licence.

What is changed, all in RensaioBackend:
1. Services/Series/DiskChapterScanner.cs (new) and AddSeriesAsync: chapters already in the series folder count as downloaded when a series is added. They are never fetched again, gaps are still filled.
1b. Services/Series/ChapterNameParser.cs: the file-name reader, with a check in tools/tests/ParserCheck (`dotnet run`).
2. Services/Downloads/DownloadsExtensions.cs: a permanent source also skips chapters any other source already holds, so a chapter comes from one site only.
3. Services/Downloads/DownloadCommandService.cs: QueueChapterDownloadsAsync skips a chapter another source is already downloading; RescheduleDownloadAsync fails over to the next healthy source that lists the chapter (TryFailoverAsync, ChapterDownload.TriedProviders) instead of retrying a dead one for hours.
4d. Services/Downloads/ChapterFilter.cs (new) and DownloadsExtensions.cs: only real chapters are downloaded (whole numbers, no bonus/extra/one-shot/volume names). RENSAIO_ALLOW_FRACTIONAL_CHAPTERS=true allows x.5 chapters. Checked in tools/tests/ParserCheck.
4c. Services/Background/JobQueueHostedService.cs: a download job that runs longer than 45 minutes is stopped, counted as failed and retried, so one hung site cannot hold every download slot for ever.
4b. Services/Downloads/GapRepair.cs (new) and DownloadsExtensions.cs: chapters missing between the lowest and highest one you own (and not on disk) are fetched, up to 25 per run. Pure rule, checked in tools/tests/ParserCheck.
4. Services/Series/SeriesCommandService.cs UpdateSourceAsync: a source that fails 15 titles in a row stops the pass (it used to retry tens of thousands of titles).

tools/kaizoku_smart_add.py: add a series from the best source, from the command line.

Image: ghcr.io/hikki-dev/rensaio:latest (amd64 and arm64), built by .github/workflows/image.yml on every change to the app.
Run it like the upstream image, for example:

  docker run -d --name kaizoku -p 9833:9833 -e PUID=1000 -e PGID=1000 -v /path/config:/config -v /path/manga:/series ghcr.io/hikki-dev/rensaio:latest

From the original Kaizoku 2.0 image: stop it, back up the config folder, and start this image on the same folders. The database is migrated on first start.
