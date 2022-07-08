using ScrapperAspNet.Controllers;
using Microsoft.AspNetCore.Mvc;
using ScrapperLibrary.Models;
using ScrapperLibrary.Services;
using System.Runtime.InteropServices;
using System.Diagnostics;
using ScrapperBlazorLibrary.Services;

namespace ScrapperAspNet.Main;
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