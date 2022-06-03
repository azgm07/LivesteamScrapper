using LivesteamScrapper.Controllers;
using LivesteamScrapper.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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
CancellationTokenSource cts = new();
WatcherController watcherController = new(EnumsModel.ScrapperMode.Viewers, cts.Token);
List<string> lines = FileController.ReadCsv("files/config", "streams.txt");

if (!lines.Any())
{
    ConsoleController.ShowWarningLog("Program", "Config file is empty, waiting for entries on web browser.");
}
_ = Task.Run(() => watcherController.StreamingWatcherAsync(lines));

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
