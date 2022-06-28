﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scrapper.Services;

namespace ScrapperLibrary.Services
{
    public static class ServiceConfiguration
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedService<HostService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IWatcherService, WatcherService>();
            services.AddScoped<IBrowserService, BrowserService>();
            services.AddScoped<IScrapperInfoService, ScrapperInfoService>();
            services.AddScoped<ITimeService, TimeService>();

            //services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(60));
        }

        public static void ConfigureLogging(ILoggingBuilder logging)
        {
            logging.ClearProviders();
            logging.AddConsole();
        }
    }
}
