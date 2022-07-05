using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scrapper.Models;
using ScrapperLibrary.Interfaces;
using System.Collections.Concurrent;
using System.Timers;
using static Scrapper.Models.EnumsModel;

namespace Scrapper.Services;

public interface IWatcherService
{
    bool AddStream(string website, string channelPath, bool start = true);
    ScrapperStatus GetUpdatedStatus(string website, string channelPath);
    bool RemoveStream(string website, string channelPath, bool saveFile = true);
    void StartAllStreamScrapper();
    Task<bool> StartStreamScrapperAsync(string website, string channelPath);
    void StopAllStreamScrapper();
    Task<bool> StopStreamScrapperAsync(string website, string channelPath);
    public bool StartStream(string website, string channelPath);
    public bool StopStream(string website, string channelPath);
    Task StreamingWatcherAsync(List<string> streams, CancellationToken token);
    public List<Stream> ListStreams { get; }
    public int SecondsToWait { get; set; }
    public CancellationToken CancellationToken { get; }

    public delegate void AddStreamEventHandler(string website, string channelPath);
    public static event AddStreamEventHandler? AddStreamEvent;
    public delegate void RemoveStreamEventHandler(string website, string channelPath);
    public static event RemoveStreamEventHandler? RemoveStreamEvent;
    public delegate void StartAllStreamEventHandler();
    public static event StartAllStreamEventHandler? StartAllStreamEvent;
    public delegate void StopAllStreamEventHandler();
    public static event StopAllStreamEventHandler? StopAllStreamEvent;
    public delegate void StartStreamEventHandler(string website, string channelPath);
    public static event StartStreamEventHandler? StartStreamEvent;
    public delegate void StopStreamEventHandler(string website, string channelPath);
    public static event StopStreamEventHandler? StopStreamEvent;
}

public class WatcherService : IWatcherService
{
    public List<Stream> ListStreams { get; private set; }
    public int SecondsToWait { get; set; }

    public CancellationToken CancellationToken { get; private set; }
    private readonly ILogger<WatcherService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFileService _file;
    private readonly IProcessService _processService;
    private bool isReady;

    public event IWatcherService.AddStreamEventHandler? AddStreamEvent;
    public event IWatcherService.RemoveStreamEventHandler? RemoveStreamEvent;
    public event IWatcherService.StartAllStreamEventHandler? StartAllStreamEvent;
    public event IWatcherService.StopAllStreamEventHandler? StopAllStreamEvent;
    public event IWatcherService.StartStreamEventHandler? StartStreamEvent;
    public event IWatcherService.StopStreamEventHandler? StopStreamEvent;

    public WatcherService(IServiceScopeFactory scopeFactory, ILogger<WatcherService> logger, IFileService file, IProcessService processService, int secondsToWait = 300)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _file = file;
        _processService = processService;
        CancellationToken = new();
        ListStreams = new();
        SecondsToWait = secondsToWait;
        isReady = false;
    }

    public bool AddStream(string website, string channelPath, bool start = true)
    {
        AddStreamEvent?.Invoke(website, channelPath);

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
                Stream stream = new(website, channelPath, environment, _scopeFactory, SecondsToWait);
                if (start)
                {
                    stream.Status = ScrapperStatus.Waiting;
                }
                else
                {
                    stream.Status = ScrapperStatus.Stopped;
                }

                ListStreams.Add(stream);
                SaveCurrentStreams();

                if (start)
                {
                    Func<Task> func = new(() => StartStreamScrapperAsync(website, channelPath));
                    FuncProcess funcProcess = new(index, OperationProcess.StartStream, func);
                    _processService.StartQueue.Enqueue(funcProcess);
                }
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
        StartStreamEvent?.Invoke(website, channelPath);

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
                Func<Task> func = new(() => StartStreamScrapperAsync(website, channelPath));
                FuncProcess funcProcess = new(index, OperationProcess.StartStream, func);
                _processService.StartQueue.Enqueue(funcProcess);
                ListStreams[index].Status = ScrapperStatus.Waiting;
                return true;
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
        StopStreamEvent?.Invoke(website, channelPath);

        try
        {
            int index = ListStreams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
            if (index < 0)
            {
                return false;
            }
            else
            {
                ListStreams[index].WaitTimer.Stop();
                Func<Task> func = new(() => StopStreamScrapperAsync(website, channelPath));
                FuncProcess funcProcess = new(index, OperationProcess.StopStream, func);
                _processService.StopQueue.Enqueue(funcProcess);
                ListStreams[index].Status = ScrapperStatus.Stopped;
                return true;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to stop the streaming");
            return false;
        }

    }

    public bool RemoveStream(string website, string channelPath, bool saveFile = true)
    {
        RemoveStreamEvent?.Invoke(website, channelPath);

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
                ListStreams[index].Dispose();
                ListStreams.RemoveAt(index);
                if(saveFile)
                {
                    SaveCurrentStreams();
                }
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
                    result = await ListStreams[index].Scrapper.RunScrapperAsync(ListStreams[index].Environment, ListStreams[index].Channel, index);
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
                if (ListStreams[index].Scrapper != null)
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
        StartAllStreamEvent?.Invoke();

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
                        stream.Status = ScrapperStatus.Waiting;
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
        StopAllStreamEvent?.Invoke();

        if (!isReady)
        {
            return;
        }

        try
        {
            _processService.RemoveProcessQueue(_processService.StartQueue);
            _processService.RemoveProcessQueue(_processService.RunQueue);
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
            _processService.RemoveProcessQueue(_processService.StartQueue);
            _processService.RemoveProcessQueue(_processService.RunQueue);
            List<Action> actions = new();
            foreach (var stream in ListStreams)
            {
                actions.Add(() =>
                {
                    RemoveStream(stream.Website, stream.Channel, false);
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
                AddStream(str[0], str[1], false);
            }
        }
    }    

    private void SaveCurrentStreams()
    {
        List<string> lines = new();
        foreach (var stream in ListStreams)
        {
            lines.Add($"{stream.Website},{stream.Channel}");
        }

        _file.WriteCsv("config", "streams.txt", lines, true);
    }

    public async Task StreamingWatcherAsync(List<string> streams, CancellationToken token)
    {
        //Start process
        Task processTask = Task.Run(() => _processService.ProcessQueueAsync(token), CancellationToken.None);

        try
        {
            CancellationToken = token;
            isReady = true;

            //Load streams with string of "website,channel"
            await Task.Run(() => AddStreamEntries(streams), CancellationToken.None);

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

            //Add event
            Stream.ChangeScrapperStatusEvent += Stream_ChangeScrapperStatusEvent;
            Stream.ElapsedOnceEvent += Stream_ElapsedOnceEvent;

            //Check and update
            while (!CancellationToken.IsCancellationRequested)
            {
                if (ListStreams.Count > 0)
                {
                    DateTime start = DateTime.Now;
                    List<Stream> streamsCopy = new(ListStreams);

                    //Debug create new time
                    if (flush)
                    {
                        try
                        {
                            await Task.Run(() => debugData["Times"].Add($"{DateTime.Now:dd/MM/yyyy HH:mm:ss}"), CancellationToken.None);

                            foreach (var stream in streamsCopy)
                            {
                                //Debug add status
                                await Task.Run(() =>
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
                                }, CancellationToken.None);
                            }

                            //Debug flush out
                            //Debug create new time
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
                        catch (TaskCanceledException)
                        {
                            _logger.LogWarning("StreamingWatcherAsync in WatcherService was cancelled");
                        }
                        catch (Exception e)
                        {
                            _logger.LogCritical(e, "StreamingWatcherAsync in WatcherService throwed an exception");
                        }
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
            //Remove events
            Stream.ChangeScrapperStatusEvent -= Stream_ChangeScrapperStatusEvent;
            Stream.ElapsedOnceEvent -= Stream_ElapsedOnceEvent;

            isReady = false;

            //Salve current streams
            await Task.Run(() => SaveCurrentStreams(), CancellationToken.None);

            //Finish task
            await Task.Run(() => AbortAllStreamScrapper(), CancellationToken.None);

            await processTask;
        }
    }

    private void Stream_ElapsedOnceEvent(Stream stream)
    {
        if (stream.Status == ScrapperStatus.Waiting)
        {
            stream.WaitTimer.Stop();
            StartStream(stream.Website, stream.Channel);
        }
    }

    private void Stream_ChangeScrapperStatusEvent(Stream stream)
    {
        //Execution
        if (stream.Status == ScrapperStatus.Running)
        {
            if (stream.Scrapper != null && !stream.Scrapper.IsScrapping)
            {
                stream.Status = ScrapperStatus.Waiting;
                stream.WaitTimer.Start();
            }
        }
        else if (stream.Status == ScrapperStatus.Stopped && stream.WaitTimer != null && stream.WaitTimer.Enabled)
        {
            stream.WaitTimer.Stop();
        }
    }
}

public sealed class Stream : IDisposable
{
    public string Website { get; set; }
    public string Channel { get; set; }
    public EnvironmentModel Environment { get; set; }
    private readonly IServiceScope _scope;
    public IScrapperService Scrapper
    {
        get
        {
            IScrapperService service = (IScrapperService)_scope.ServiceProvider.GetRequiredService(typeof(IScrapperService));
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
            ChangeScrapperStatusEvent?.Invoke(this);
        }
    }
    public TimerPlus WaitTimer { get; set; }

    public delegate void ChangeScrapperStatusEventHandler(Stream stream);

    public static event ChangeScrapperStatusEventHandler? ChangeScrapperStatusEvent;

    public delegate void ElapsedOnceEventHandler(Stream stream);

    public static event ElapsedOnceEventHandler? ElapsedOnceEvent;

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
        WaitTimer.ElapsedOnceEvent += WaitTimer_ElapsedOnceEvent;
        Scrapper.StatusChangeEvent += Scrapper_StatusChangeEvent;
    }

    private void Scrapper_StatusChangeEvent()
    {
        ChangeScrapperStatusEvent?.Invoke(this);
    }

    private void WaitTimer_ElapsedOnceEvent()
    {
        ElapsedOnceEvent?.Invoke(this);
    }

    public void Dispose()
    {
        WaitTimer.ElapsedOnceEvent -= WaitTimer_ElapsedOnceEvent;
        Scrapper.StatusChangeEvent -= Scrapper_StatusChangeEvent;
        Status = ScrapperStatus.Stopped;
        Scrapper.Stop();
        Scrapper.Dispose();
        WaitTimer?.Dispose();
    }
}

public sealed class TimerPlus : System.Timers.Timer, IDisposable
{
    private DateTime m_dueTime;
    private bool _elapsedOnce;
    public bool ElapsedOnce
    {
        get
        {
            return _elapsedOnce;
        }
        set
        {
            _elapsedOnce = value;
            if (value)
            {
                ElapsedOnceEvent?.Invoke();
            }
        }
    }
    public TimerPlus(int timer) : base(timer) => this.Elapsed += ElapsedAction;

    public delegate void ElapsedOnceEventHandler();

    public event ElapsedOnceEventHandler? ElapsedOnceEvent;

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
