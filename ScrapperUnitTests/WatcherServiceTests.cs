using Xunit;
using Scrapper.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Scrapper.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Hosting;

namespace ScrapperUnitTests
{
    public class WatcherServiceTests
    {
        [Theory]
        [InlineData("twitch", "gaules")]
        public async Task AddStream(string website, string channel)
        {
            bool result = true;

            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddHostedService<HostService>();
                services.AddSingleton<IFileService, FileService>();
                services.AddSingleton<IWatcherService, WatcherService>();
                services.AddScoped<IBrowserService, BrowserService>();
                services.AddScoped<IScrapperInfoService, ScrapperInfoService>();
                services.AddScoped<ITimeService, TimeService>();
            });

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });

            var app = builder.Build();

            //Execution
            var watcher = app.Services.GetService<IWatcherService>();

            if (watcher != null)
            {
                await Task.Run(() => watcher.AddStream(website, channel));
                await Task.Delay(1000);
                if (watcher.ListStreams.FindIndex(stream => stream.Website == website && stream.Channel == channel) < 0)
                {
                    result = false;
                }
            }

            Assert.True(result);
        }

        [Theory]
        [InlineData("twitch", "gaules")]
        public async Task RemoveStream(string website, string channel)
        {
            bool result = true;

            //Build
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddHostedService<HostService>();
                services.AddSingleton<IFileService, FileService>();
                services.AddSingleton<IWatcherService, WatcherService>();
                services.AddScoped<IBrowserService, BrowserService>();
                services.AddScoped<IScrapperInfoService, ScrapperInfoService>();
                services.AddScoped<ITimeService, TimeService>();
            });

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });

            var app = builder.Build();

            //Execution
            var watcher = app.Services.GetService<IWatcherService>();

            if (watcher != null)
            {                
                await Task.Run(() => watcher.AddStream(website, channel));
                await Task.Delay(1000);
                await Task.Run(() => watcher.RemoveStream(website, channel));
                await Task.Delay(1000);
                if (watcher.ListStreams.FindIndex(stream => stream.Website == website && stream.Channel == channel) >= 0)
                {
                    result = false;
                }
            }

            Assert.True(result);
        }
    }
}