using LivesteamScrapper.Models;
using static LivesteamScrapper.Models.EnumsModel;

namespace LivesteamScrapper.Services
{
    public interface IWatcherService
    {
        bool AddStream(string website, string channelPath);
        ScrapperStatus GetUpdatedStatus(string website, string channelPath);
        bool RemoveStream(string website, string channelPath);
        void SaveCurrentStreams();
        void StartAllStreamScrapper();
        Task<bool> StartStreamScrapperAsync(string website, string channelPath);
        void StopAllStreamScrapper();
        Task<bool> StopStreamScrapperAsync(string website, string channelPath);
        Task StreamingWatcherAsync(List<string> streams, ScrapperMode mode, CancellationToken token);
    }

    public class WatcherService : IWatcherService
    {
        private readonly List<Stream> listStreams;

        private CancellationToken ct;
        private ScrapperMode scrapperMode;
        private readonly ILogger<WatcherService> _logger;
        private readonly IServiceProvider _provider;
        private readonly IFileService _file;

        public WatcherService(IServiceProvider provider, ILogger<WatcherService> logger, IFileService file)
        {
            _logger = logger;
            _provider = provider;
            _file = file;
            ct = new();
            scrapperMode = new();
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
                    Stream stream = new(website, channelPath, environment, _provider);
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
                _logger.LogError("AddStream", e);
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
                    listStreams[index].Scrapper.Stop();
                    listStreams[index].Dispose();
                    listStreams.RemoveAt(index);
                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("RemoveStream", e);
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
                                result = await listStreams[index].Scrapper.RunViewerScrapperAsync(listStreams[index].Environment, listStreams[index].Channel);
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
                _logger.LogError("StartStreamScrapperAsync", e);
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
                _logger.LogError("StopStreamScrapperAsync", e);
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
                _logger.LogError("StartAllStreamScrapper", e);
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
                _logger.LogError("StopAllStreamScrapper", e);
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

            _file.WriteCsv("files/config", "streams.txt", lines, true);
        }

        public async Task StreamingWatcherAsync(List<string> streams, ScrapperMode mode, CancellationToken token)
        {
            scrapperMode = mode;
            ct = token;

            //Load streams with string of "website,channel"
            await Task.Run(() => AddStreamEntries(streams), CancellationToken.None);
            await Task.Delay(5000, CancellationToken.None);

            //Setup Debugger
            Dictionary<string, List<string>> debugData = new();
            debugData.Add($"Times", new List<string>());
            int debugCounter = 10;

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
                        if (debugCounter <= 0)
                        {
                            await Task.Run(() => debugData["Times"].Add($"{DateTime.Now:dd/MM/yyyy HH:mm:ss}"), CancellationToken.None);
                        }

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
                            //Debug create new time
                            if (debugCounter <= 0)
                            {
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
                                }, CancellationToken.None));
                            }
                        }

                        await Task.WhenAll(debugTasks);

                        //Debug flush out
                        //Debug create new time
                        if (debugCounter <= 0)
                        {
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
                                    _file.WriteCsv("files/debug", "status.csv", lines, true);
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError("StreamingWatcherAsync->Debug", e);
                                }
                            }, CancellationToken.None);
                            debugCounter = 10;
                        }

                        debugCounter--;
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
                    _logger.LogError("StreamingWatcherAsync", e);
                }
            }

            //Salve current streams
            SaveCurrentStreams();

            //Finish task
            await Task.Run(() => StopAllStreamScrapper(), CancellationToken.None);
        }
    }

    sealed class Stream : IDisposable
    {
        public string Website { get; set; }
        public string Channel { get; set; }
        public EnvironmentModel Environment { get; set; }
        private readonly IServiceScope _scope;
        public IScrapperService Scrapper 
        {
            get
            {
                return (IScrapperService)_scope.ServiceProvider.GetRequiredService(typeof(IScrapperService));
            }
        }
        public ScrapperStatus Status { get; set; }
        public DateTime WaitTime { get; set; }

        public Stream(string website, string channel, EnvironmentModel environment, IServiceProvider provider)
        {
            Website = website;
            Channel = channel;
            Environment = environment;
            _scope = provider.CreateScope();
            Status = ScrapperStatus.Stopped;
            WaitTime = new DateTime();
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
