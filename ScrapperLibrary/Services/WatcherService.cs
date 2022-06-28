using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scrapper.Models;
using System.Collections.Concurrent;
using System.Timers;
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
    public CancellationToken CancellationToken { get; }
}

public class WatcherService : IWatcherService
{
    public List<Stream> ListStreams { get; private set; }
    public int SecondsToWait { get; set; }

    public CancellationToken CancellationToken { get; private set; }
    private ScrapperMode scrapperMode;
    private readonly ILogger<WatcherService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFileService _file;
    private bool isReady;
    private readonly ConcurrentQueue<Func<Task>> processQueue;
    private readonly Task processStreamStack;

    public WatcherService(IServiceScopeFactory scopeFactory, ILogger<WatcherService> logger, IFileService file, int secondsToWait = 300)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _file = file;
        CancellationToken = new();
        scrapperMode = ScrapperMode.Delayed;
        ListStreams = new();
        SecondsToWait = secondsToWait;
        isReady = false;
        processQueue = new();

        processStreamStack = Task.Run(() => ProcessStreamStackAsync());
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
                Stream stream = new(website, channelPath, environment, _scopeFactory, SecondsToWait)
                {
                    Status = ScrapperStatus.Waiting
                };
                ListStreams.Add(stream);
                Func<Task> func = new(() => StartStreamScrapperAsync(website, channelPath));
                processQueue.Enqueue(func);
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

    public bool StartStream(string website, string channelPath)
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
                Func<Task> func = new(() => StartStreamScrapperAsync(website, channelPath));
                processQueue.Enqueue(func);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start the streaming");
            return false;
        }

    }

    public bool StopStream(string website, string channelPath)
    {
        try
        {
            int index = ListStreams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
            if (index < 0)
            {
                Func<Task> func = new(() => StopStreamScrapperAsync(website, channelPath));
                processQueue.Enqueue(func);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to stop the streaming");
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
                Func<Task> func = new(() => new Task<bool>(bool () =>
                {
                    try
                    {
                        ListStreams[index].Status = ScrapperStatus.Stopped;
                        if(ListStreams[index].Scrapper != null)
                        {
                            ListStreams[index].Scrapper.Stop();
                        }
                        ListStreams.RemoveAt(index);
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }));
                processQueue.Enqueue(func);
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
                if (ListStreams[index].Scrapper != null && !ListStreams[index].Scrapper.IsScrapping)
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
                    ListStreams[index].WaitTimer.Start();
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
                if(ListStreams[index].Scrapper != null)
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
                if (stream.Scrapper != null && !stream.Scrapper.IsScrapping)
                {
                    actions.Add(() =>
                    {
                        StartStream(stream.Website, stream.Channel);
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
                    StopStream(stream.Website, stream.Channel);
                });
            }
            Parallel.Invoke(actions.ToArray());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Stop all the scrappers throwed an exception");
        }
    }

    public void AbortAllStreamScrapper()
    {
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
        }
    }

    private async Task ProcessStreamStackAsync()
    {
        try
        {
            try
            {
                await Task.Delay(5000, CancellationToken);
            }
            catch (Exception)
            {
                //Wrap task delay cancelled
            }
            while (true)
            {
                List<Task> tasks = new();

                for (int i = 0; i < 5; i++)
                {
                    if (processQueue.TryDequeue(out Func<Task>? func) && func != null)
                    {
                        tasks.Add(Task.Run(func, CancellationToken));
                    }
                }

                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception)
                {
                    //Wrap task whenall cancelled
                }

                if(CancellationToken.IsCancellationRequested && processQueue.IsEmpty)
                {
                    break;
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Main stack ProcessStreamStackAsync finished with error");
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
        try
        {
            scrapperMode = mode;
            CancellationToken = token;
            isReady = true;

            //Load streams with string of "website,channel"
            await Task.Run(() => AddStreamEntries(streams), CancellationToken.None);
            await Task.Delay(5000, CancellationToken.None);

            //Setup Debugger
            Dictionary<string, List<string>> debugData = new()
            {
                { $"Times", new List<string>() }
            };

            bool flush = false;

            using System.Timers.Timer timer = new(600000);
            timer.Elapsed += (sender, e) => flush = true;
            timer.AutoReset = true;
            timer.Start();

            int watcherDelay = 1000;

            //Check and update
            while (!CancellationToken.IsCancellationRequested)
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
                            if (stream.WaitTimer.Enabled && stream.WaitTimer.ElapsedOnce)
                            {
                                stream.WaitTimer.Start();
                                StartStream(stream.Website, stream.Channel);
                            }
                        }
                        else if (stream.Status == ScrapperStatus.Running)
                        {
                            if (stream.Scrapper != null && !stream.Scrapper.IsScrapping)
                            {
                                stream.Status = ScrapperStatus.Waiting;
                                stream.WaitTimer.Start();
                            }
                        }
                        else if (stream.Status == ScrapperStatus.Stopped && stream.WaitTimer.Enabled)
                        {
                            stream.WaitTimer.Stop();
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
                                List<string> lines = new()
                                {
                                    $"Streams,{string.Join(",", debugCopy["Times"])}"
                                };
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
                    await Task.Delay(delay, CancellationToken);
                }
                else
                {
                    await Task.Delay(watcherDelay, CancellationToken);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Main watcher for scrappers throwed an exception");
        }
        finally
        {
            isReady = false;

            //Salve current streams
            await Task.Run(() => SaveCurrentStreams(), CancellationToken.None);

            //Finish task
            await Task.Run(() => AbortAllStreamScrapper(), CancellationToken.None);

            Task stack = processStreamStack;
            await stack;
        }
    }
}

public sealed class Stream
{
    public string Website { get; set; }
    public string Channel { get; set; }
    public EnvironmentModel Environment { get; set; }
    private readonly IServiceScope _scope;
    public IScrapperInfoService Scrapper
    {
        get
        {
            IScrapperInfoService service = (IScrapperInfoService)_scope.ServiceProvider.GetRequiredService(typeof(IScrapperInfoService));
            return service;

        }
    }
    private ScrapperStatus _status;
    public ScrapperStatus Status
    {
        get
        {
            return _status;
        }
        set
        {
            _status = value;
            ChangeScrapperEvent?.Invoke(value);
        }
    }
    public TimerPlus WaitTimer { get; set; }

    public delegate void ChangeScrapperStatusEventHandler(ScrapperStatus status);

    public static event ChangeScrapperStatusEventHandler? ChangeScrapperEvent;

    public Stream(string website, string channel, EnvironmentModel environment, IServiceScopeFactory scopeService, int timer)
    {
        Website = website;
        Channel = channel;
        Environment = environment;
        _scope = scopeService.CreateScope();
        Status = ScrapperStatus.Stopped;
        WaitTimer = new(timer * 1000)
        {
            AutoReset = true
        };
    }
}

public sealed class TimerPlus : System.Timers.Timer, IDisposable
{
    private DateTime m_dueTime;

    public bool ElapsedOnce { get; set; }
    public TimerPlus(int timer) : base(timer) => this.Elapsed += ElapsedAction;

    public new void Dispose()
    {
        this.Elapsed -= ElapsedAction;
        base.Dispose();
    }

    public double TimeLeft
    {
        get
        {
            if (this.Enabled)
            {
                return (this.m_dueTime - DateTime.Now).TotalMilliseconds;
            }
            else
            {
                return 0;
            }
        }
    }
    public new void Start()
    {
        this.m_dueTime = DateTime.Now.AddMilliseconds(this.Interval);
        ElapsedOnce = false;
        base.Start();
    }

    public new void Stop()
    {
        ElapsedOnce = false;
        base.Stop();
    }

    private void ElapsedAction(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (this.AutoReset)
        {
            ElapsedOnce = true;
            this.m_dueTime = DateTime.Now.AddMilliseconds(this.Interval);
        }
    }
}
