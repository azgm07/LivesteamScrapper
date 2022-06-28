// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scrapper.Services;
using ScrapperLibrary.Services;
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
                ServiceConfiguration.ConfigureServices(services);
                services.AddWpfBlazorWebView();
                services.AddSingleton(typeof(MainWindow));
            });

            builder.ConfigureLogging(logging =>
            {
                ServiceConfiguration.ConfigureLogging(logging);
            });

            builder.UseConsoleLifetime();

            _host = builder.Build();

            ServiceProvider = _host.Services;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                MessageBox.Show(error.ExceptionObject.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            
            base.OnStartup(e);

            if (ServiceProvider != null)
            {
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }

            await _host.StartAsync();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                MessageBox.Show(error.ExceptionObject.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            
            base.OnExit(e);
            
            await _host.StopAsync();
        }
    }
}

