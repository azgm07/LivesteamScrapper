using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace LivesteamScrapper.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            CancellationTokenSource cts = new();
            WatcherController watcherController = new(_logger, EnumsModel.ScrapperMode.Viewers, cts.Token);
            List<string> lines = FileController.ReadCsv("files/config", "streams.txt");

            List<Task> tasks = new();
            tasks.Add(watcherController.StreamingWatcherAsync(lines));

            tasks.Add(Task.Run(async () => 
            {
                //await Task.Delay(75000);
                //watcherController.AddStream("booyah", "leet");
                await Task.Delay(90000);
                cts.Cancel();

            }, CancellationToken.None));

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}