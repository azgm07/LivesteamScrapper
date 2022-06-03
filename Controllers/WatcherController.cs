﻿using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using static LivesteamScrapper.Models.EnumsModel;

namespace LivesteamScrapper.Controllers
{
    public class WatcherController : Controller
    {
        private readonly List<Stream> listStreams;

        private readonly CancellationToken ct;
        private readonly ILogger<HomeController> _logger;
        private readonly ScrapperMode scrapperMode;

        public WatcherController(ILogger<HomeController> logger, ScrapperMode mode, CancellationToken token)
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
                    ScrapperController scrapperController = new(_logger, environment, channelPath);
                    Stream stream = new(website, channelPath, environment, scrapperController);
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

        private async Task<bool> StartStreamScrapperAsync(string website, string channelPath)
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
                    return result;
                }
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog("StartStreamScrapperAsync", e.Message);
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
                            Task.Run(() => 
                                StartStreamScrapperAsync(stream.Website, stream.Channel)
                        ));
                    }
                }
                Parallel.Invoke(actions.ToArray());
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog("StartAllStreamScrapper", e.Message);
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
                    if (listStreams[index].Scrapper.IsScrapping)
                    {
                        await Task.Run(() => listStreams[index].Scrapper.Stop());
                        listStreams[index].Status = ScrapperStatus.Stopped;
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

        public void StopAllStreamScrapper()
        {
            try
            {
                List<Action> actions = new();
                foreach (var stream in listStreams)
                {
                    actions.Add(() =>
                    {
                        stream.Scrapper.Stop();
                        stream.Status = ScrapperStatus.Stopped;
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
            }
        }

        public void SaveCurrentStreams()
        {
            List<string> lines = new();
            foreach (var stream in listStreams)
            {
                lines.Add($"{stream.Website},{stream.Channel}");
            }
            FileController.WriteCsv("files/config", "streams.txt", lines);
        }

        public async Task StreamingWatcherAsync(List<string> streams)
        {
            //Load streams with string of "website,channel"
            await Task.Run(() => AddStreamEntries(streams));

            //Check and update
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (listStreams.Count > 0)
                    {
                        List<Stream> streamsCopy = new(listStreams);
                        foreach (var stream in streamsCopy)
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
                            else if (stream.Status == ScrapperStatus.Stopped)
                            {
                                if (stream.Scrapper.IsScrapping)
                                {
                                    stream.Status = ScrapperStatus.Running;
                                }
                            }
                            else if (stream.Status == ScrapperStatus.Waiting)
                            {
                                TimeSpan timeSpan = DateTime.Now - stream.WaitTime;
                                if (timeSpan.TotalSeconds >= 600)
                                {
                                    stream.WaitTime = new DateTime();
                                    await StartStreamScrapperAsync(stream.Website, stream.Channel);
                                }
                            }

                            await Task.Delay(60000 / listStreams.Count, ct);
                        }
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
