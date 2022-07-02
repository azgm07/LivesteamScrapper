using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scrapper.Services;
using ScrapperLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrapperBlazorLibrary.Services
{
    public static class BlazorConfiguration
    {
        public static void ConfigureServices(WebApplicationBuilder builder)
        {
            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddControllersWithViews();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddHostedService<HostService>();
            builder.Services.AddSingleton<IFileService, FileService>();
            builder.Services.AddSingleton<IWatcherService, WatcherService>();
            builder.Services.AddSingleton<IProcessService, ProcessService>();
            builder.Services.AddScoped<IBrowserService, BrowserService>();
            builder.Services.AddScoped<IScrapperService, ScrapperProcessService>();
            builder.Services.AddScoped<ITimeService, TimeService>();
            builder.Services.AddSingleton<IWatcherService, WatcherService>();
            builder.Services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(60));
        }

        public static void ConfigureLogging(WebApplicationBuilder builder)
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddDebug();
        }
    }
}
