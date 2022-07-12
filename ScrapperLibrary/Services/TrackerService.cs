using Microsoft.Extensions.Logging;
using ScrapperLibrary.Controllers;
using ScrapperLibrary.Interfaces;
using ScrapperLibrary.Models;
using ScrapperLibrary.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrapperLibrary.Services
{
    public class TrackerService : ITrackerService
    {
        public List<InstanceController> TrackerInstances { get; private set; }
        public int TrackerSeconds { get; set; }
        public int WaitSeconds { get; set; }
        public CancellationToken CurrentToken { get; private set; }

        private readonly QueueController _queueController;
        private readonly IFileService _fileService;
        private readonly ILogger<TrackerService> _logger;
        private readonly ILoggerFactory _loggerFactory;


        public TrackerService(ILogger<TrackerService> logger, IFileService fileService, ILoggerFactory loggerFactory)
        {
            TrackerInstances = new();
            _fileService = fileService;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _queueController = new(loggerFactory.CreateLogger<QueueController>());
            TrackerSeconds = 60;
            WaitSeconds = 300;
            CurrentToken = new();
        }

        public async Task RunTrackerAsync(List<string> streams, CancellationToken token)
        {
            CurrentToken = token;

            //Start process
            Task queueTask = Task.Run(() => _queueController.RunQueueAsync(token), CancellationToken.None);

            try
            {
                await Task.Run(() => AddInstances(streams), CancellationToken.None);

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
                while (!token.IsCancellationRequested)
                {
                    if (TrackerInstances.Count > 0)
                    {
                        DateTime start = DateTime.Now;
                        List<InstanceController> streamsCopy = new(TrackerInstances);

                        //Debug create new time
                        if (flush)
                        {
                            try
                            {
                                await Task.Run(() => debugData["Times"].Add($"{DateTime.Now:dd/MM/yyyy HH:mm:ss}"), CancellationToken.None);

                                //Debug add status if channel already registered or create new
                                foreach (InstanceController? stream in streamsCopy)
                                {
                                    await Task.Run(() =>
                                    {
                                        if (!debugData.ContainsKey($"{stream.Tracker.Website},{stream.Tracker.Channel}"))
                                        {
                                            debugData.Add($"{stream.Tracker.Website},{stream.Tracker.Channel}", new List<string>());
                                        }
                                        for (int i = debugData[$"{stream.Tracker.Website},{stream.Tracker.Channel}"].Count; i < debugData["Times"].Count - 1; i++)
                                        {
                                            debugData[$"{stream.Tracker.Website},{stream.Tracker.Channel}"].Add("");
                                        }
                                        debugData[$"{stream.Tracker.Website},{stream.Tracker.Channel}"].Add(stream.Status.ToString());
                                    }, CancellationToken.None);
                                }

                                //Debug flush out
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
                                        _fileService.WriteFile("files/debug", "status.csv", lines, true);
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
                        await Task.Delay(delay, token);
                    }
                    else
                    {
                        await Task.Delay(watcherDelay, token);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Main watcher for scrappers throwed an exception");
            }
            finally
            {
                //Salve current streams
                await Task.Run(() => SaveCurrentStreams(), CancellationToken.None);

                //Finish task
                await Task.Run(() => RemoveAllInstances(), CancellationToken.None);

                await queueTask;
            }
        }

        public bool AddInstance(string website, string channel, bool start = true, bool save = false)
        {
            try
            {
                int index = TrackerInstances.FindIndex(instance => instance.Tracker.Website == website &&
                            instance.Tracker.Channel == channel);
                if (index < 0)
                {
                    TrackerController tracker = new(StreamEnvironment.GetEnvironment(website), channel, _loggerFactory, _fileService);
                    InstanceController instance = new(TrackerInstances.Count, Enums.StreamStatus.Stopped, tracker, TrackerSeconds, WaitSeconds);
                    TrackerInstances.Add(instance);
                    instance.CallRunEvent += Instance_CallRunEvent;

                    if (save)
                    {
                        SaveCurrentStreams();
                    }

                    if (start)
                    {
                        instance.Start();
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
                _logger.LogError(e, "Failed to add instance");
                return false;
            }
        }

        public bool RemoveInstance(string website, string channel, bool saveFile = true)
        {
            try
            {
                int index = TrackerInstances.FindIndex(instance => instance.Tracker.Website == website &&
                            instance.Tracker.Channel == channel);
                if (index < 0)
                {
                    return false;
                }
                else
                {
                    TrackerInstances[index].Dispose();
                    TrackerInstances.RemoveAt(index);
                    if (saveFile)
                    {
                        SaveCurrentStreams();
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to remove instance");
                return false;
            }
        }

        public bool StartInstance(string website, string channel)
        {
            try
            {
                int index = TrackerInstances.FindIndex(instance => instance.Tracker.Website == website &&
                            instance.Tracker.Channel == channel);
                if (index < 0)
                {
                    return false;
                }
                else
                {
                    TrackerInstances[index].Start();
                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to start the streaming");
                return false;
            }
        }

        public bool StopInstance(string website, string channel)
        {
            try
            {
                int index = TrackerInstances.FindIndex(instance => instance.Tracker.Website == website &&
                            instance.Tracker.Channel == channel);
                if (index < 0)
                {
                    return false;
                }
                else
                {
                    TrackerInstances[index].Stop();
                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to start the streaming");
                return false;
            }
        }

        public void StartAllInstances()
        {
            try
            {
                _queueController.RemoveFromQueue(_queueController.ProcessQueue);
                List<Action> actions = new();
                foreach (var tracker in TrackerInstances.Select(tracker => tracker.Tracker))
                {
                    actions.Add(() =>
                    {
                        StartInstance(tracker.CurrentEnvironment.Website, tracker.Channel);
                    });
                }
                Parallel.Invoke(actions.ToArray());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Start all instances throwed an exception");
            }
        }

        public void StopAllInstances()
        {
            try
            {
                _queueController.RemoveFromQueue(_queueController.ProcessQueue);
                List<Action> actions = new();
                foreach (var tracker in TrackerInstances.Select(tracker => tracker.Tracker))
                {
                    actions.Add(() =>
                    {
                        StopInstance(tracker.CurrentEnvironment.Website, tracker.Channel);
                    });
                }
                Parallel.Invoke(actions.ToArray());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Stop all instances throwed an exception");
            }
        }

        private void AddInstances(List<string> streamers)
        {
            foreach (var stream in streamers)
            {
                string[] str = stream.Split(',');
                if (str.Length > 1 && !string.IsNullOrEmpty(str[0]) && !string.IsNullOrEmpty(str[1]))
                {
                    AddInstance(str[0], str[1], false);
                }
            }
        }

        private void RemoveAllInstances()
        {
            try
            {
                _queueController.RemoveFromQueue(_queueController.ProcessQueue);
                List<Action> actions = new();
                foreach (var tracker in TrackerInstances.Select(tracker => tracker.Tracker))
                {
                    actions.Add(() =>
                    {
                        RemoveInstance(tracker.CurrentEnvironment.Website, tracker.Channel, false);
                    });
                }
                Parallel.Invoke(actions.ToArray());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Remove all instances throwed an exception");
            }
        }

        private void SaveCurrentStreams()
        {
            List<string> lines = new();
            foreach (var tracker in TrackerInstances.Select(tracker => tracker.Tracker))
            {
                lines.Add($"{tracker.CurrentEnvironment.Website},{tracker.Channel}");
            }

            _fileService.WriteFile("config", "streams.txt", lines, true);
        }

        private void Instance_CallRunEvent(InstanceController instance)
        {
            Func<Task> func = new(() => instance.Tracker.GetInfoAsync(CurrentToken));
            QueueFunc queueFunc = new(instance.Index, func);
            _queueController.ProcessQueue.Enqueue(queueFunc);
        }
    }
}
