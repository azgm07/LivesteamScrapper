using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Scrapper.Models;
using Scrapper.Services;
using ScrapperBlazorLibrary.Data;

namespace ScrapperBlazor.Main;


public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddSingleton<WeatherForecastService>();
        builder.Services.AddSingleton<AppData>();
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
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }
}