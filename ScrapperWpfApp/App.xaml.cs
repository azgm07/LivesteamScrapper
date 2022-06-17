﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scrapper.Services;
using System;
using System.Threading;
using System.Windows;

namespace ScrapperWpfApp
{
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }
        public static IConfiguration? Configuration { get; private set; }
        private static readonly CancellationTokenSource _mainCTS = new();

        public static CancellationToken MainCancellationToken 
        { 
            get 
            {
                return _mainCTS.Token;
            }
        }

        private void AppStartup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                MessageBox.Show(error.ExceptionObject.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            var builder = new ConfigurationBuilder();

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<HostService, HostService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IWatcherService, WatcherService>();
            services.AddScoped<IBrowserService, BrowserService>();
            services.AddScoped<IScrapperInfoService, ScrapperInfoService>();
            services.AddScoped<ITimeService, TimeService>();
            services.AddWpfBlazorWebView();
            services.AddTransient(typeof(MainWindow));
        }

        private void AppExit(object sender, ExitEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                MessageBox.Show(error.ExceptionObject.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            _mainCTS.Cancel();
        }
    }
}

