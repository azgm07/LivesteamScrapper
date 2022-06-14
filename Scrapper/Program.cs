using Microsoft.Extensions.DependencyInjection;
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
            services.AddHostedService<WatcherService>();
            services.AddScoped<IBrowserService, BrowserService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddScoped<IScrapperInfoService, ScrapperInfoService>();
            services.AddScoped<ITimeService, TimeService>();
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });

        var app = builder.Build();

        app.Run();
    }
}

