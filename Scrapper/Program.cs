﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scrapper.Models;
using Scrapper.Services;

namespace Scrapper.Main;
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);
        builder.ConfigureServices(services =>
        {
            services.AddHostedService<RunWatcherService>();
            services.AddScoped<IBrowserService, BrowserService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddScoped<IScrapperInfoService, ScrapperInfoService>();
            services.AddScoped<ITimeService, TimeService>();
            services.AddSingleton<IWatcherService, WatcherService>();
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });

        var app = builder.Build();

        app.Run();
    }

    internal sealed class RunWatcherService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IFileService _fileService;
        private readonly IWatcherService _watcherService;

        public RunWatcherService(ILogger<RunWatcherService> logger, IHostApplicationLifetime appLifetime, 
            IFileService fileService, IWatcherService watcherService)
        {
            _logger = logger;
            _appLifetime = appLifetime;
            _fileService = fileService;
            _watcherService = watcherService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                List<string> lines = _fileService.ReadCsv("files/config", "streams.txt");

                if (!lines.Any())
                {
                    _logger.LogWarning("Config file is empty, waiting for entries on web browser.");
                }
                await _watcherService.StreamingWatcherAsync(lines, EnumsModel.ScrapperMode.Delayed, cancellationToken);
            }
            catch (Exception e)
            {

                _logger.LogError(e, "Unhandled exception!");
            }
            finally
            {
                // Stop the application once the work is done
                _appLifetime.StopApplication();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

