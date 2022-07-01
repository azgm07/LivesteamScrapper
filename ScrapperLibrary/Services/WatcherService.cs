using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scrapper.Models;
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
    private readonly ConcurrentQueue<FuncProcess> processQueue;
    private readonly Task processQueueTask;

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

        processQueueTask = Task.Run(() => ProcessQueueAsync());
    }

    public bool AddStream(string website, string channelPath, bool start = true)
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
                    processQueue.Enqueue(funcProcess);
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
                processQueue.Enqueue(funcProcess);
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
        try
        {
            int index = ListStreams.FindIndex(stream => stream.Website == website && stream.Channel == channelPath);
            if (index < 0)
            {
                return false;
            }
            else
            {
                Func<Task> func = new(() => StopStreamScrapperAsync(website, channelPath));
                FuncProcess funcProcess = new(index, OperationProcess.StopStream, func);
                processQueue.Enqueue(funcProcess);
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
        if (!isReady)
        {
            return;
        }

        try
        {
            RemoveProcessQueue();
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
            RemoveProcessQueue();
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

    private async Task ProcessQueueAsync()
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
                List<FuncProcess> listFunc = new();

                for (int i = 0; i < 10; i++)
                {
                    if (processQueue.TryDequeue(out FuncProcess? process) && process != null)
                    {
                        listFunc.Add(process);
                        if (process.Operation == OperationProcess.StopStream)
                        {
                            Task task = Task.Run(() => RemoveProcessQueue(OperationProcess.StartStream, process.Index));
                            await task;
                        }
                    }
                }

                List<Task> tasks = new();
                foreach (var item in listFunc)
                {
                    tasks.Add(Task.Run(item.FuncTask, CancellationToken));
                }

                await Task.WhenAll(tasks);

                //Break if shutdown was requested
                if (CancellationToken.IsCancellationRequested && processQueue.IsEmpty)
                {
                    break;
                }

                await Task.Delay(1000, CancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("ProcessStreamStackAsync in WatcherService was cancelled");
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "ProcessStreamStackAsync in WatcherService finished with error");
        }
    }
    private void RemoveProcessQueue()
    {
        //Remove all
        while (!processQueue.IsEmpty)
        {
            if (processQueue.TryDequeue(out FuncProcess? process) && process != null)
            {
                _logger.LogInformation("Dequeued process from {index}, operation {operation}", process.Index, process.Operation);
            }
        }
    }
    private void RemoveProcessQueue(OperationProcess operation, int index)
    {
        if (index >= 0)
        {
            for (int i = 0; i < processQueue.Count; i++)
            {
                if (processQueue.TryDequeue(out FuncProcess? process) && process != null)
                {
                    if (process.Index != index)
                    {
                        processQueue.Enqueue(process);
                    }
                    else if (process.Operation != operation)
                    {
                        processQueue.Enqueue(process);
                    }
                    else
                    {
                        _logger.LogInformation("Dequeued process from {index}, operation {operation}", process.Index, process.Operation);
                    }
                }
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
            isReady = false;

            //Salve current streams
            await Task.Run(() => SaveCurrentStreams(), CancellationToken.None);

            //Finish task
            await Task.Run(() => AbortAllStreamScrapper(), CancellationToken.None);

            Task queue = processQueueTask;
            await queue;
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

    public class FuncProcess
    {
        public int Index { get; set; }
        public OperationProcess Operation { get; set; }
        public Func<Task> FuncTask { get; set; }

        public FuncProcess(int index, OperationProcess operation, Func<Task> funcTask)
        {
            Index = index;
            Operation = operation;
            FuncTask = funcTask;
        }
    }

    public enum OperationProcess
    {
        StartStream,
        StopStream
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
    }

    private void WaitTimer_ElapsedOnceEvent()
    {
        ElapsedOnceEvent?.Invoke(this);
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
