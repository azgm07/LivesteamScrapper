using Scrapper.Controllers;
using Microsoft.AspNetCore.Mvc;
using Scrapper.Models;
using Scrapper.Services;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Main;
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

        //Execution
        var logger = app.Services.GetRequiredService<ILogger<Controller>>();
        CancellationTokenSource cts = new();

        var file = app.Services.GetService<IFileService>();
        var watcher = app.Services.GetService<IWatcherService>();

        if (file != null && watcher != null)
        {
            List<string> lines = file.ReadCsv("files/config", "streams.txt");

            if (!lines.Any())
            {
                logger.LogWarning("Config file is empty, waiting for entries on web browser.");
            }
            _ = Task.Run(() => watcher.StreamingWatcherAsync(lines, EnumsModel.ScrapperMode.Delayed, cts.Token));
        }

        app.Run();
    }
}