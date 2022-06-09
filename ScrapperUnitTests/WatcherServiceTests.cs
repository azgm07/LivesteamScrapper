using Xunit;
using Scrapper.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Scrapper.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace ScrapperUnitTests
{
    public class WatcherServiceTests
    {
        [Theory]
        [InlineData("twitch", "gaules")]
        public async Task AddStream(string website, string channel)
        {
            bool result = true;

            //Build
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            // Add services
            builder.Services.AddScoped<IBrowserService, BrowserService>();
            builder.Services.AddSingleton<IFileService, FileService>();
            builder.Services.AddScoped<IScrapperService, ScrapperService>();
            builder.Services.AddScoped<ITimeService, TimeService>();
            builder.Services.AddSingleton<IWatcherService, WatcherService>();

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
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            // Add services
            builder.Services.AddScoped<IBrowserService, BrowserService>();
            builder.Services.AddSingleton<IFileService, FileService>();
            builder.Services.AddScoped<IScrapperService, ScrapperService>();
            builder.Services.AddScoped<ITimeService, TimeService>();
            builder.Services.AddSingleton<IWatcherService, WatcherService>();

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