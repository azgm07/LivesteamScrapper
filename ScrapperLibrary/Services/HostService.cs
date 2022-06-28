using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scrapper.Models;

namespace Scrapper.Services
{
    public class HostService : BackgroundService
    {        
        private readonly IWatcherService _watcherService;
        private readonly IFileService _fileService;

        public HostService(ILogger<HostService> logger, IHostApplicationLifetime appLifetime, IWatcherService watcherService, IFileService fileService) : base(logger, appLifetime)
        {
            _fileService = fileService;
            _watcherService = watcherService;
        }

        public override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                List<string> lines = _fileService.ReadCsv("files/config", "streams.txt");

                if (!lines.Any())
                {
                    _logger.LogWarning("Config file is empty, waiting for entries on web browser.");
                }
                return _watcherService.StreamingWatcherAsync(lines, EnumsModel.ScrapperMode.Delayed, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unhandled exception!");
                return Task.CompletedTask;
            }
        }
    }
}
