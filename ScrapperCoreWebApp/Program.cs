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
        builder.Services.AddHostedService<HostService>();
        builder.Services.AddSingleton<IFileService, FileService>();
        builder.Services.AddSingleton<IWatcherService, WatcherService>();
        builder.Services.AddScoped<IBrowserService, BrowserService>();
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
}