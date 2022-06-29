using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scrapper.Models;
using Scrapper.Services;
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
        Task task = Task.Run(async () =>
        {
            Console.WriteLine("3 seconds to start all...");
            await Task.Delay(3000);
            watcher.StartAllStreamScrapper();
        });

        await appTask;
    }
}

