using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scrapper.Models;
using static Scrapper.Models.EnumsModel;

namespace Scrapper.Services;

public interface IWatcherService
{
    bool AddStream(string website, string channelPath);
    ScrapperStatus GetUpdatedStatus(string website, string channelPath);
    bool RemoveStream(string website, string channelPath);
    void StartAllStreamScrapper();
    Task<bool> StartStreamScrapperAsync(string website, string channelPath);
    void StopAllStreamScrapper();
    Task<bool> StopStreamScrapperAsync(string website, string channelPath);
    Task StreamingWatcherAsync(List<string> streams, ScrapperMode mode, CancellationToken token);
    public List<Stream> ListStreams { get; }
    public int SecondsToWait { get; set; }
}

public class WatcherService : IWatcherService
{
    public List<Stream> ListStreams { get; private set; }
    public int SecondsToWait { get; set; }

    private CancellationToken ct;
    private ScrapperMode scrapperMode;
    private readonly ILogger<WatcherService> _logger;
    private readonly IServiceProvider _provider;
    private readonly IFileService _file;
    private bool isReady;

    public WatcherService(IServiceProvider provider, ILogger<WatcherService> logger, IFileService file, int secondsToWait = 300)
    {
        _logger = logger;
        _provider = provider;
        _file = file;
        ct = new();
        scrapperMode = ScrapperMode.Delayed;
        ListStreams = new();
        SecondsToWait = secondsToWait;
        isReady = false;
    }

    public bool AddStream(string website, string channelPath)
    {
        if (!isReady)
        {
            return false;
        }

        try
        {
            int index = ListStreams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
            if (index < 0)
            {
                EnvironmentModel environment = EnvironmentModel.GetEnvironment(website);
                Stream stream = new(website, channelPath, environment, _provider);
                stream.Status = ScrapperStatus.Running;
                ListStreams.Add(stream);
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
            _logger.LogError(e, "Failed to add the streaming");
            return false;
        }

    }

    public bool RemoveStream(string website, string channelPath)
    {
        if (!isReady)
        {
            return false;
        }

        try
        {
            int index = ListStreams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
            if (index < 0)
            {
                return false;
            }
            else
            {
                ListStreams[index].Status = ScrapperStatus.Stopped;
                ListStreams[index].Scrapper.Stop();
                ListStreams[index].Dispose();
                ListStreams.RemoveAt(index);
                return true;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to remove the streaming");
            return false;
        }

    }

    public ScrapperStatus GetUpdatedStatus(string website, string channelPath)
    {
        if (!isReady)
        {
            return ScrapperStatus.NotFound;
        }

        int index = ListStreams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
        if (index < 0)
        {
            return ScrapperStatus.NotFound;
        }
        else
        {
            return ListStreams[index].Status;
        }
    }

    public async Task<bool> StartStreamScrapperAsync(string website, string channelPath)
    {
        if (!isReady)
        {
            return false;
        }

        try
        {
            bool result = false;
            int index = ListStreams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
            if (index < 0)
            {
                return false;
            }
            else
            {
                ListStreams[index].Status = ScrapperStatus.Running;


                if (!ListStreams[index].Scrapper.IsScrapping)
                {
                    result = await ListStreams[index].Scrapper.RunScrapperAsync(ListStreams[index].Environment, ListStreams[index].Channel, scrapperMode);
                }
                if (result)
                {
                    ListStreams[index].Status = ScrapperStatus.Running;
                }
                else
                {
                    ListStreams[index].Status = ScrapperStatus.Waiting;
                    ListStreams[index].WaitTime = DateTime.Now;
                }

                return result;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Run the scrapper throwed an exception");
            return false;
        }
    }

    public async Task<bool> StopStreamScrapperAsync(string website, string channelPath)
    {
        if (!isReady)
        {
            return false;
        }

        try
        {
            int index = ListStreams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
            if (index < 0)
            {
                return false;
            }
            else
            {
                ListStreams[index].Status = ScrapperStatus.Stopped;
                if (ListStreams[index].Scrapper.IsScrapping)
                {
                    await Task.Run(() => ListStreams[index].Scrapper.Stop());
                }
                return true;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Stop the scrapper throwed an exception");
            return false;
        }
    }

    public void StartAllStreamScrapper()
    {
        if (!isReady)
        {
            return;
        }

        try
        {
            List<Action> actions = new();
            foreach (var stream in ListStreams)
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
            _logger.LogError(e, "Start all the scrappers throwed an exception");
        }
    }

    public void StopAllStreamScrapper()
    {
        if (!isReady)
        {
            return;
        }

        try
        {
            List<Action> actions = new();
            foreach (var stream in ListStreams)
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
            _logger.LogError(e, "Stop all the scrappers throwed an exception");
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

    private void SaveCurrentStreams()
    {
        List<string> lines = new();
        foreach (var stream in ListStreams)
        {
            lines.Add($"{stream.Website},{stream.Channel}");
        }

        _file.WriteCsv("files/config", "streams.txt", lines, true);
    }

    public async Task StreamingWatcherAsync(List<string> streams, ScrapperMode mode, CancellationToken token)
    {
        scrapperMode = mode;
        ct = token;
        isReady = true;

        //Load streams with string of "website,channel"
        await Task.Run(() => AddStreamEntries(streams), CancellationToken.None);
        await Task.Delay(5000, CancellationToken.None);

        //Setup Debugger
        Dictionary<string, List<string>> debugData = new();
        debugData.Add($"Times", new List<string>());

        bool flush = false;

        using System.Timers.Timer timer = new(600000);
        timer.Elapsed += (sender, e) => flush = true;
        timer.AutoReset = true;
        timer.Start();

        int watcherDelay = 60000;

        //Check and update
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (ListStreams.Count > 0)
                {
                    DateTime start = DateTime.Now;
                    List<Stream> streamsCopy = new(ListStreams);
                    List<Task> debugTasks = new();

                    //Debug create new time
                    if (flush)
                    {
                        await Task.Run(() => debugData["Times"].Add($"{DateTime.Now:dd/MM/yyyy HH:mm:ss}"), CancellationToken.None);                        
                    }

                    foreach (var stream in streamsCopy)
                    {
                        //Execution
                        if (stream.Status == ScrapperStatus.Waiting)
                        {
                            TimeSpan timeSpan = DateTime.Now - stream.WaitTime;
                            if (timeSpan.TotalSeconds >= SecondsToWait)
                            {
                                _ = Task.Run(() => StartStreamScrapperAsync(stream.Website, stream.Channel));
                                await Task.Delay(1000, CancellationToken.None);
                            }
                        }
                        else if (stream.Status == ScrapperStatus.Running && !stream.Scrapper.IsScrapping)
                        {
                            stream.Status = ScrapperStatus.Waiting;
                            stream.WaitTime = DateTime.Now;
                        }

                        //Debug add status
                        //Debug create new time
                        if (flush)
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
                    if (flush)
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
                            catch (Exception)
                            {
                                _logger.LogWarning("Failed to write the debugger for scrappers status.");
                            }
                        }, CancellationToken.None);
                        flush = false;
                    }

                    int delay = watcherDelay - (int)(DateTime.Now - start).TotalMilliseconds;
                    await Task.Delay(delay, ct);
                }
                else
                {
                    await Task.Delay(watcherDelay, ct);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Main watcher for scrappers throwed an exception.");
            }
        }

        isReady = false;
        
        //Salve current streams
        SaveCurrentStreams();

        //Finish task
        await Task.Run(() => StopAllStreamScrapper(), CancellationToken.None);
    }
}

public sealed class Stream : IDisposable
{
    public string Website { get; set; }
    public string Channel { get; set; }
    public EnvironmentModel Environment { get; set; }
    private readonly IServiceScope _scope;
    public IScrapperInfoService Scrapper
    {
        get
        {
            return (IScrapperInfoService)_scope.ServiceProvider.GetRequiredService(typeof(IScrapperInfoService));
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
