using LivesteamScrapper.Controllers;
using LivesteamScrapper.Models;
using LivesteamScrapper.Services;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

// Add services
builder.Services.AddScoped<IBrowserService, BrowserService>();
builder.Services.AddSingleton<IFileService, FileService>();
builder.Services.AddScoped<IScrapperService, ScrapperService>();
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

if(file != null && watcher != null)
{
    List<string> lines = file.ReadCsv("files/config", "streams.txt");

    if (!lines.Any())
    {
        logger.LogWarning("Config file is empty, waiting for entries on web browser.");
    }
    _ = Task.Run(() => watcher.StreamingWatcherAsync(lines, EnumsModel.ScrapperMode.Viewers, cts.Token));
}

//Test Task
//tasks.Add(Task.Run(async () =>
//{
//    await Task.Delay(300000);
//    //Console.WriteLine("\n-------------------> Adding new Stream\n");
//    //await Task.Run(() => watcherController.AddStream("twitch", "yetz"));
//    //await Task.Delay(180000);
//    //Console.WriteLine("\n-------------------> Remove new Stream\n");
//    //await Task.Run(() => watcherController.RemoveStream("twitch", "yetz"));
//    //await Task.Delay(180000);
//    //Console.WriteLine("\n-------------------> Start new Stream\n");
//    //await Task.Run(() => watcherController.AddStream("twitch", "yetz"));
//    //await Task.Delay(180000);
//    //Console.WriteLine("\n-------------------> Cancelling the watcher execution\n");
//    cts.Cancel();

//}, CancellationToken.None));

app.Run();
