using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;
using static LivesteamScrapper.Models.EnumsModel;

namespace LivesteamScrapper.Controllers
{
    public class WatcherController : Controller
    {
        private static readonly List<Stream> streams = new();

        private readonly CancellationToken ct;
        private readonly ILogger<HomeController> _logger;

        public WatcherController(ILogger<HomeController> logger, CancellationToken token)
        {
            _logger = logger;
            ct = token;
        }

        public bool AddStream(string website, string channelPath)
        {
            try
            {
                int index = streams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
                if (index < 0)
                {
                    EnvironmentModel environment = EnvironmentModel.GetEnvironment(website);
                    ScrapperController scrapperController = new(_logger, environment, channelPath);
                    Stream stream = new(website, channelPath, environment, scrapperController);
                    streams.Add(stream);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
                return false;
            }
        }

        public bool RemoveStream(string website, string channelPath)
        {
            try
            {
                int index = streams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
                if (index < 0)
                {
                    return false;
                }
                else
                {
                    streams[index].Scrapper.Dispose();
                    streams.RemoveAt(index);
                    return true;
                }
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
                return false;
            }

        }

        public ScrapperStatus GetUpdatedStatus(string website, string channelPath)
        {
            int index = streams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
            if (index < 0)
            {
                return ScrapperStatus.NotFound;
            }
            else
            {
                return streams[index].Status;
            }
        }

        public async Task<bool> StartStreamScrapperAsync(ScrapperMode scrapperMode, string website, string channelPath)
        {
            try
            {
                int index = streams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
                if (index < 0)
                {
                    return false;
                }
                else
                {
                    if (!streams[index].Scrapper.IsScrapping)
                    {
                        switch (scrapperMode)
                        {
                            case ScrapperMode.Off:
                                break;
                            case ScrapperMode.Viewers:
                                await streams[index].Scrapper.RunViewerScrapperAsync();
                                break;
                            case ScrapperMode.Chat:
                                break;
                            case ScrapperMode.All:
                                break;
                            default:
                                break;
                        }
                        streams[index].Status = ScrapperStatus.Running;
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
                return false;
            }
        }

        public async Task StartAllStreamScrapperAsync(ScrapperMode scrapperMode)
        {
            try
            {
                List<Task> tasks = new();
                foreach (var stream in streams)
                {
                    if (!stream.Scrapper.IsScrapping)
                    {
                        switch (scrapperMode)
                        {
                            case ScrapperMode.Off:
                                break;
                            case ScrapperMode.Viewers:
                                tasks.Add(
                                    Task.Run(stream.Scrapper.RunViewerScrapperAsync).ContinueWith((result) => 
                                    {
                                        if (result.Result)
                                        {
                                            stream.Status = ScrapperStatus.Running;
                                        }
                                        else
                                        {
                                            stream.Status = ScrapperStatus.Waiting;
                                        }
                                    }
                                    ,TaskScheduler.Default)
                                );
                                break;
                            case ScrapperMode.Chat:
                                break;
                            case ScrapperMode.All:
                                break;
                            default:
                                break;
                        }
                        stream.Status = ScrapperStatus.Running;
                    }
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
            }
        }

        public async Task<bool> StopStreamScrapperAsync(string website, string channelPath)
        {
            try
            {
                int index = streams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
                if (index < 0)
                {
                    return false;
                }
                else
                {
                    if (streams[index].Scrapper.IsScrapping)
                    {
                        await Task.Run(() => streams[index].Scrapper.Stop());
                        streams[index].Status = ScrapperStatus.Stopped;
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
                return false;
            }
        }

        public async Task StopAllStreamScrapperAsync()
        {
            List<Task> tasks = new();
            foreach (var stream in streams)
            {
                if (stream.Scrapper.IsScrapping)
                {
                    tasks.Add(Task.Run(() => stream.Scrapper.Stop()));
                    stream.Status = ScrapperStatus.Stopped;
                }
            }
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
            }
        }

        public void AddStreamEntries(List<(string, string)> streamers)
        {
            foreach (var (website, channel) in streamers)
            {
                AddStream(website, channel);
            }
        }

        public async Task StreamingWatcherAsync(ScrapperMode mode, List<(string, string)>? streamers = null)
        {
            //Load streams or get from parameters
            if(streamers != null)
            {
                await Task.Run(() => AddStreamEntries(streamers));
            }
            Task task = StartAllStreamScrapperAsync(mode);

            //Check and update
            while (!ct.IsCancellationRequested)
            {
                if (streams.Count > 0)
                {
                    foreach (var stream in streams)
                    {
                        if (stream.Status == ScrapperStatus.Running)
                        {
                            if (stream.Scrapper.IsScrapping)
                            {
                                stream.Status = ScrapperStatus.Running;
                            }
                            else
                            {
                                stream.Status = ScrapperStatus.Waiting;
                                stream.WaitTime = DateTime.Now;
                            }
                        }
                        else if (stream.Status == ScrapperStatus.Waiting)
                        {
                            TimeSpan timeSpan = DateTime.Now - stream.WaitTime;
                            if (timeSpan.TotalSeconds >= 600)
                            {
                                stream.WaitTime = new DateTime();
                                await StartStreamScrapperAsync(mode, stream.Website, stream.Channel);
                            }
                        }

                        await Task.Delay(60000 / streams.Count);
                    }
                }
                else
                {
                    await Task.Delay(60000);
                }
            }
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
