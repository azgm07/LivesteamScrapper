// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using ScrapperLibrary.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ScrapperLibrary;

namespace ScrapperWpfApp
{
    public partial class App : Application
    {
        private readonly IHost _host;
        public static IServiceProvider? ServiceProvider { get; private set; }
        private Task? mainTask;

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

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                MessageBox.Show(error.ExceptionObject.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            
            if (ServiceProvider != null)
            {
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();

                var process = ServiceProvider.GetRequiredService<IProcessService>();
                for (int i = 0; i < e.Args.Length-1; i++)
                {
                    if (e.Args[i] == "-thread" && int.TryParse(e.Args[i+1], out int value))
                    {
                        process.Threads = value;
                    }
                }
            }

            mainTask = _host.RunAsync();


            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                MessageBox.Show(error.ExceptionObject.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            _host.StopAsync();
            if(mainTask != null)
            {
                mainTask.Wait();
            }
            base.OnExit(e);
        }
    }
}

