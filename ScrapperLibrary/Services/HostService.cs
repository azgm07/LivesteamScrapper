using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScrapperLibrary.Interfaces;
using ScrapperLibrary.Models;

namespace ScrapperLibrary.Services
{
    public class HostService : BackgroundService
    {
        private readonly ITrackerService _trackerService;
        private readonly IFileService _fileService;
        private readonly ILogger<HostService> _logger;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public HostService(IHostApplicationLifetime hostApplicationLifetime, ILogger<HostService> logger, ITrackerService trackerService, IFileService fileService)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
            _fileService = fileService;
            _trackerService = trackerService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                try
                {
                    List<string> lines = _fileService.ReadFile("config", "streams.txt");

                    if (!lines.Any())
                    {
                        _logger.LogWarning("Config file is empty, waiting for entries on web browser.");
                    }
                    await _trackerService.RunTrackerAsync(lines, stoppingToken);
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
