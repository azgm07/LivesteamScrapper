// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scrapper.Services;
using System;
using System.Threading;
using System.Windows;

namespace ScrapperWpfApp
{
    public partial class App : Application
    {
        private readonly IHost _host;
        public static IServiceProvider? ServiceProvider { get; private set; }

        public App()
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddHostedService<HostService>();
                services.AddSingleton<IFileService, FileService>();
                services.AddSingleton<IWatcherService, WatcherService>();
                services.AddScoped<IBrowserService, BrowserService>();
                services.AddScoped<IScrapperInfoService, ScrapperInfoService>();
                services.AddScoped<ITimeService, TimeService>();
                services.AddWpfBlazorWebView();
                services.AddSingleton(typeof(MainWindow));

                services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(60));
            });

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });

            builder.UseConsoleLifetime();

            _host = builder.Build();

            ServiceProvider = _host.Services;
        }

        private async void AppStartup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                MessageBox.Show(error.ExceptionObject.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            if (ServiceProvider != null)
            {
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }

            try
            {
                await _host.RunAsync();
            }
            catch (OperationCanceledException)
            {
                // suppress
            }
        }

        private async void AppExit(object sender, ExitEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                MessageBox.Show(error.ExceptionObject.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            using (_host)
            {
                await _host.StopAsync();
            }
        }
    }
}

