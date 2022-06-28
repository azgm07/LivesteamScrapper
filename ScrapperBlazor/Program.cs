using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Scrapper.Models;
using Scrapper.Services;
using ScrapperBlazorLibrary.Data;
using ScrapperBlazorLibrary.Services;

namespace ScrapperBlazor.Main;


public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        BlazorConfiguration.ConfigureServices(builder);
        BlazorConfiguration.ConfigureLogging(builder);

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