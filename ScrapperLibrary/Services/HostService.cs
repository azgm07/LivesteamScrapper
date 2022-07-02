using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scrapper.Models;

namespace Scrapper.Services
{
    public class HostService : BackgroundService
    {
        private readonly IWatcherService _watcherService;
        private readonly IFileService _fileService;
        private readonly ILogger<HostService> _logger;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public HostService(IHostApplicationLifetime hostApplicationLifetime, ILogger<HostService> logger, IWatcherService watcherService, IFileService fileService)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
            _fileService = fileService;
            _watcherService = watcherService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                try
                {
                    List<string> lines = _fileService.ReadCsv("config", "streams.txt");

                    if (!lines.Any())
                    {
                        _logger.LogWarning("Config file is empty, waiting for entries on web browser.");
                    }
                    await _watcherService.StreamingWatcherAsync(lines, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Fatal error");
                    throw;
                }
                finally
                {
                    _hostApplicationLifetime.StopApplication();
                }
            }, CancellationToken.None);
        }
    }
}
