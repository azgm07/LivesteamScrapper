using ScrapperAspNet.Controllers;
using Microsoft.AspNetCore.Mvc;
using Scrapper.Models;
using Scrapper.Services;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ScrapperAspNet.Main;
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddControllersWithViews();

        // Add services
        builder.Services.AddHostedService<RunWatcherService>();
        builder.Services.AddScoped<IBrowserService, BrowserService>();
        builder.Services.AddSingleton<IFileService, FileService>();
        builder.Services.AddScoped<IScrapperInfoService, ScrapperInfoService>();
        builder.Services.AddScoped<ITimeService, TimeService>();
        builder.Services.AddSingleton<IWatcherService, WatcherService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }

    internal sealed class RunWatcherService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IFileService _fileService;
        private readonly IWatcherService _watcherService;

        public RunWatcherService(ILogger<RunWatcherService> logger, IHostApplicationLifetime appLifetime,
            IFileService fileService, IWatcherService watcherService)
        {
            _logger = logger;
            _appLifetime = appLifetime;
            _fileService = fileService;
            _watcherService = watcherService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                List<string> lines = _fileService.ReadCsv("files/config", "streams.txt");

                if (!lines.Any())
                {
                    _logger.LogWarning("Config file is empty, waiting for entries on web browser.");
                }
                await _watcherService.StreamingWatcherAsync(lines, EnumsModel.ScrapperMode.Delayed, cancellationToken);
            }
            catch (Exception e)
            {

                _logger.LogError(e, "Unhandled exception!");
            }
            finally
            {
                // Stop the application once the work is done
                _appLifetime.StopApplication();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}