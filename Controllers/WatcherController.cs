using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;
using static LivesteamScrapper.Models.EnumsModel;

namespace LivesteamScrapper.Controllers
{
    public class WatcherController : Controller
    {
        private readonly List<Stream> listStreams;

        private readonly CancellationToken ct;
        private readonly ILogger<Controller>? _logger;
        private readonly ScrapperMode scrapperMode;

        public WatcherController(ScrapperMode mode, CancellationToken token, ILogger<Controller>? logger = null)
        {
            _logger = logger;
            ct = token;
            scrapperMode = mode;
            listStreams = new();
        }

        public bool AddStream(string website, string channelPath)
        {
            try
            {
                int index = listStreams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
                if (index < 0)
                {
                    EnvironmentModel environment = EnvironmentModel.GetEnvironment(website);
                    ScrapperController scrapperController = new(environment, channelPath, _logger);
                    Stream stream = new(website, channelPath, environment, scrapperController);
                    stream.Status = ScrapperStatus.Running;
                    listStreams.Add(stream);
                    _ = Task.Run(() => StartStreamScrapperAsync(website, channelPath));
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog("AddStream", e.Message);
                return false;
            }
        }

        public bool RemoveStream(string website, string channelPath)
        {
            try
            {
                int index = listStreams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
                if (index < 0)
                {
                    return false;
                }
                else
                {
                    listStreams[index].Status = ScrapperStatus.Stopped;
                    listStreams[index].Scrapper.Dispose();
                    listStreams.RemoveAt(index);
                    return true;
                }
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog("RemoveStream", e.Message);
                return false;
            }

        }

        public ScrapperStatus GetUpdatedStatus(string website, string channelPath)
        {
            int index = listStreams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
            if (index < 0)
            {
                return ScrapperStatus.NotFound;
            }
            else
            {
                return listStreams[index].Status;
            }
        }

        public async Task<bool> StartStreamScrapperAsync(string website, string channelPath)
        {
            try
            {
                bool result = false;
                int index = listStreams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
                if (index < 0)
                {
                    return false;
                }
                else
                {
                    listStreams[index].Status = ScrapperStatus.Running;

                    if (!listStreams[index].Scrapper.IsScrapping)
                    {
                        switch (scrapperMode)
                        {
                            case ScrapperMode.Off:
                                break;
                            case ScrapperMode.Viewers:
                                result = await listStreams[index].Scrapper.RunViewerScrapperAsync();
                                break;
                            case ScrapperMode.Chat:
                                break;
                            case ScrapperMode.All:
                                break;
                            default:
                                break;
                        }
                    }

                    if (result)
                    {
                        listStreams[index].Status = ScrapperStatus.Running;
                    }
                    else
                    {
                        listStreams[index].Status = ScrapperStatus.Waiting;
                        listStreams[index].WaitTime = DateTime.Now;
                    }

                    return result;
                }
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog("StartStreamScrapperAsync", e.Message);
                return false;
            }
        }

        public async Task<bool> StopStreamScrapperAsync(string website, string channelPath)
        {
            try
            {
                int index = listStreams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
                if (index < 0)
                {
                    return false;
                }
                else
                {
                    listStreams[index].Status = ScrapperStatus.Stopped;
                    if (listStreams[index].Scrapper.IsScrapping)
                    {
                        await Task.Run(() => listStreams[index].Scrapper.Stop());
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog("StopStreamScrapperAsync", e.Message);
                return false;
            }
        }

        public void StartAllStreamScrapper()
        {
            try
            {
                List<Action> actions = new();
                foreach (var stream in listStreams)
                {
                    if (!stream.Scrapper.IsScrapping)
                    {
                        actions.Add(() =>
                        {
                            _ = Task.Run(() => StartStreamScrapperAsync(stream.Website, stream.Channel));
                        }
                        );
                    }
                }
                Parallel.Invoke(actions.ToArray());
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog("StartAllStreamScrapper", e.Message);
            }
        }

        public void StopAllStreamScrapper()
        {
            try
            {
                List<Action> actions = new();
                foreach (var stream in listStreams)
                {
                    actions.Add(() =>
                    {
                        _ = Task.Run(() => StopStreamScrapperAsync(stream.Website, stream.Channel));
                    });
                }
                Parallel.Invoke(actions.ToArray());
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog("StopAllStreamScrapper", e.Message);
            }
        }

        private void AddStreamEntries(List<string> streamers)
        {
            foreach (var stream in streamers)
            {
                string[] str = stream.Split(',');
                if (str.Length > 1 && !string.IsNullOrEmpty(str[0]) && !string.IsNullOrEmpty(str[1]))
                {
                    AddStream(str[0], str[1]);
                }
                Thread.Sleep(1000);
            }
        }

        public void SaveCurrentStreams()
        {
            List<string> lines = new();
            foreach (var stream in listStreams)
            {
                lines.Add($"{stream.Website},{stream.Channel}");
            }
            FileController.WriteCsv("files/config", "streams.txt", lines, true);
        }

        public async Task StreamingWatcherAsync(List<string> streams)
        {
            //Load streams with string of "website,channel"
            await Task.Run(() => AddStreamEntries(streams));
            await Task.Delay(5000);

            //Setup Debugger
            Dictionary<string, List<string>> debugData = new();
            debugData.Add($"Times", new List<string>());

            //Check and update
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (listStreams.Count > 0)
                    {
                        DateTime start = DateTime.Now;
                        List<Stream> streamsCopy = new(listStreams);
                        List<Task> debugTasks = new();

                        //Debug create new time
                        await Task.Run(() => debugData["Times"].Add($"{DateTime.Now:dd/MM/yyyy HH:mm:ss}"));

                        foreach (var stream in streamsCopy)
                        {
                            //Execution
                            if (stream.Status == ScrapperStatus.Waiting)
                            {
                                TimeSpan timeSpan = DateTime.Now - stream.WaitTime;
                                if (timeSpan.TotalSeconds >= 600)
                                {
                                    _ = Task.Run(() => StartStreamScrapperAsync(stream.Website, stream.Channel));
                                }
                            }

                            //Debug add status
                            debugTasks.Add(Task.Run(() =>
                            {
                                if (!debugData.ContainsKey($"{stream.Website},{stream.Channel}"))
                                {
                                    debugData.Add($"{stream.Website},{stream.Channel}", new List<string>());
                                }
                                for (int i = debugData[$"{stream.Website},{stream.Channel}"].Count; i < debugData["Times"].Count - 1; i++)
                                {
                                    debugData[$"{stream.Website},{stream.Channel}"].Add("");
                                }
                                debugData[$"{stream.Website},{stream.Channel}"].Add(stream.Status.ToString());
                            }));
                        }

                        await Task.WhenAll(debugTasks);

                        //Debug flush out
                        await Task.Run(() =>
                        {
                            try
                            {
                                Dictionary<string, List<string>> debugCopy = new(debugData);
                                List<string> lines = new();
                                lines.Add($"Streams,{string.Join(",", debugCopy["Times"])}");
                                foreach (string item in debugCopy.Keys)
                                {
                                    if (item != "Times")
                                    {
                                        lines.Add($"{item},{string.Join(",", debugCopy[item])}");
                                    }
                                }
                                FileController.WriteCsv("files/debug", "status.csv", lines, true);
                            }
                            catch (Exception e)
                            {
                                ConsoleController.ShowExceptionLog("StreamingWatcherAsync->Debug", e.Message);
                            }
                        });

                        int delay = 60000 - (int)(DateTime.Now - start).TotalMilliseconds;
                        await Task.Delay(delay, ct);
                    }
                    else
                    {
                        await Task.Delay(60000, ct);
                    }
                }
                catch (Exception e)
                {
                    ConsoleController.ShowExceptionLog("StreamingWatcherAsync", e.Message);
                }
            }

            //Salve current streams
            SaveCurrentStreams();

            //Finish task
            await Task.Run(() => StopAllStreamScrapper());
        }
    }

    internal class Stream
    {
        public string Website { get; set; }
        public string Channel { get; set; }
        public EnvironmentModel Environment { get; set; }
        public ScrapperController Scrapper { get; set; }
        public ScrapperStatus Status { get; set; }
        public DateTime WaitTime { get; set; }

        public Stream(string website, string channel, EnvironmentModel environment, ScrapperController scrapper)
        {
            Website = website;
            Channel = channel;
            Environment = environment;
            Scrapper = scrapper;
            Status = ScrapperStatus.Stopped;
            WaitTime = new DateTime();
        }
    }
}
