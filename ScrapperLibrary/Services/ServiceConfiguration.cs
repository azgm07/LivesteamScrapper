﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScrapperLibrary.Services;
using ScrapperLibrary.Interfaces;

namespace ScrapperLibrary.Services
{
    public static class ServiceConfiguration
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedService<HostService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IWatcherService, WatcherService>();
            services.AddSingleton<IProcessService, ProcessService>();
            services.AddScoped<IBrowserService, BrowserService>();
            services.AddScoped<IScrapperService, ScrapperProcessService>();
            services.AddScoped<ITimeService, TimeService>();

            services.Configure<HostOptions>(options =>
            {
                options.ShutdownTimeout = TimeSpan.FromSeconds(60);
            });
        }

        public static void ConfigureLogging(ILoggingBuilder logging)
        {
            logging.ClearProviders();
            logging.AddDebug();
            logging.AddConsole();
        }
    }
}
