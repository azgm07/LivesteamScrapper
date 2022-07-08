using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScrapperLibrary;
using ScrapperLibrary.Models;
using ScrapperLibrary.Services;

namespace Scrapper.Main;
public static class Program
{
    public async static Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);
        builder.ConfigureServices(services =>
        {
            ServiceConfiguration.ConfigureServices(services);
        });

        builder.ConfigureLogging(logging =>
        {
            ServiceConfiguration.ConfigureLogging(logging);
        });

        var app = builder.Build();

        Task appTask = app.RunAsync();
        
        var watcher = app.Services.GetRequiredService<IWatcherService>();

        _ = Task.Run(async () =>
        {
            ILogger _logger = app.Services.GetRequiredService<ILogger<HostService>>();
            _logger.LogInformation("Starting main activities in 3 seconds...");
            await Task.Delay(3000);
            watcher.StartAllStreamScrapper();
        });

        await appTask;
    }
}

