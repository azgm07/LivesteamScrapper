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
            WatcherController watcherController = new(_logger, cts.Token);
            List<(string, string)> ps = new();
            ps.Add(("booyah", "vanquilha"));
            ps.Add(("booyah", "ggeasy"));
            ps.Add(("booyah", "wanheda"));
            ps.Add(("booyah", "teus"));

            List<Task> tasks = new List<Task>();
            tasks.Add(watcherController.StreamingWatcherAsync(EnumsModel.ScrapperMode.Viewers, ps));

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