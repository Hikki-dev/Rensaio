# Private fixes (not for the public repo)

Based on maxpiva/Rensaio at 0b647ef. Push is disabled on purpose (`git remote -v`).

What is changed, all in RensaioBackend:
1. Services/Series/DiskChapterScanner.cs (new) and AddSeriesAsync: chapters already in the series folder count as downloaded when a series is added. They are never fetched again, gaps are still filled.
1b. Services/Series/ChapterNameParser.cs: the file-name reader, with a check in private/tests/ParserCheck (`dotnet run`).
2. Services/Downloads/DownloadsExtensions.cs: a permanent source also skips chapters any other source already holds, so a chapter comes from one site only.
3. Services/Downloads/DownloadCommandService.cs: QueueChapterDownloadsAsync skips a chapter another source is already downloading; RescheduleDownloadAsync fails over to the next healthy source that lists the chapter (TryFailoverAsync, ChapterDownload.TriedProviders) instead of retrying a dead one for hours.
4. Services/Series/SeriesCommandService.cs UpdateSourceAsync: a source that fails 15 titles in a row stops the pass (it used to retry tens of thousands of titles).

private/kaizoku_smart_add.py: add a series from the best source, from the command line.

Build and deploy (Mac builds, the server wraps it; no registry):
  cd RensaioFrontend && pnpm install && npm run build
  (cd out && zip -qr ../../RensaioBackend/wwwroot.zip .) ; shasum -a 256 RensaioBackend/wwwroot.zip | awk '{print toupper($1)}' > RensaioBackend/wwwroot.sha256
  cd RensaioBackend && dotnet publish RensaioBackend.csproj -c Release -r linux-x64 --self-contained false -p:PublishAot=false -p:PublishReadyToRun=false -p:DebugSymbols=false -o bin/linux/amd64
  ship bin/linux/amd64 and Dockerfile.private to the server, then: docker build -f Dockerfile.private -t kaizoku-private:local .

Previous image kept as kaizoku-private:prev on the server.
Server state: container "kaizoku" runs kaizoku-private:local; the old one is kept stopped as "kaizoku-old".
Rollback: docker stop kaizoku && docker rename kaizoku kaizoku-bad && docker rename kaizoku-old kaizoku
          restore /root/kaizoku-backup-pre-rensaio/* into /media/Docker/Kaizoku (delete rensaio.db), docker update --restart=unless-stopped kaizoku, docker start kaizoku
