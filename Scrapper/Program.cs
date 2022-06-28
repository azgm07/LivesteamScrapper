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

        await app.RunAsync();
    }
}

