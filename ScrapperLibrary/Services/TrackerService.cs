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
        public List<TrackerController> ScopedInstances { get; private set; }

        private readonly IFileService _fileService;
        private readonly ILogger<TrackerService> _logger;

        public TrackerService(ILogger<TrackerService> logger, IFileService fileService)
        {
            ScopedInstances = new();
            _fileService = fileService;
            _logger = logger;
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
    }
}
