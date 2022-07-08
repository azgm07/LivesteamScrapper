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
    public class TrackerService
    {
        public List<Instance> TrackerInstances { get; private set; }

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
        }

        private void AddInstances(List<string> streamers, CancellationToken token)
        {
            foreach (var stream in streamers)
            {
                string[] str = stream.Split(',');
                if (str.Length > 1 && !string.IsNullOrEmpty(str[0]) && !string.IsNullOrEmpty(str[1]))
                {
                    AddInstance(str[0], str[1], token, false);                    
                }
            }
        }

        public async Task RunTrackerAsync(List<string> streams, CancellationToken token)
        {
            //Start process
            Task queueTask = Task.Run(() => _queueController.RunQueueAsync(token), CancellationToken.None);

            try
            {
                await Task.Run(() => AddInstances(streams, token), CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Main watcher for scrappers throwed an exception");
            }
            finally
            {
                //Salve current streams
                //await Task.Run(() => SaveCurrentStreams(), CancellationToken.None);

                //Finish task
                //await Task.Run(() => AbortAllStreamScrapper(), CancellationToken.None);

                await queueTask;
            }
        }

        private void FlushData(StreamEnvironment environment, string channel, string currentGame, string counter)
        {
            string result = string.Concat(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), ",", currentGame, ",", counter);
            List<string> newList = new()
            {
                result
            };
            WriteData(newList, environment.Website, channel, "counters");

            CreateInfoLog(environment, channel, currentGame, counter);
        }

        private void WriteData(List<string> lines, string website, string livestream, string type, bool startNew = false)
        {
            string file = $"{ServiceUtils.RemoveSpecial(website.ToLower())}-{ServiceUtils.RemoveSpecial(livestream.ToLower())}-{type}.csv";
            _fileService.WriteFile("files/csv", file, lines, startNew);
        }
        private void CreateInfoLog(StreamEnvironment environment, string channel, string currentGame, string viewers)
        {
            StringBuilder sb = new();
            sb.Append($"Stream: {environment.Website}/{channel} | ");
            sb.Append($"Playing: {currentGame} | ");
            sb.Append($"Viewers Count: {viewers}");

            _logger.LogInformation("{message}", sb.ToString());
        }

        public bool AddInstance(string website, string channel, CancellationToken token, bool start = true)
        {
            try
            {
                int index = TrackerInstances.FindIndex(instance => instance.Tracker.CurrentEnvironment.Website == website && instance.Tracker.Channel == channel);
                if (index < 0)
                {
                    TrackerController tracker = new(StreamEnvironment.GetEnvironment(website), channel, _loggerFactory);
                    Instance instance = new(Enums.StreamStatus.Stopped, tracker);
                    TrackerInstances.Add(instance);
                    //SaveCurrentStreams();

                    if (start)
                    {
                        Func<Task> func = new(() => tracker.GetInfoAsync(token));
                        QueueFunc queueFunc = new(index, func);
                        _queueController.ProcessQueue.Enqueue(queueFunc);
                        instance.Status = Enums.StreamStatus.Running;
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
    }
}
