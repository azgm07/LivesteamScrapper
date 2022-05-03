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
            //Start tasks
            List<Task> tasks = StartScrapper("facebook", "samiraclose/videos/1011779029709268", 2);

            //Console Tasks
            Task.Run(() => ConsoleController.StartConsole(30));
            Task.Run(() => { Task.WaitAll(tasks.ToArray()); ConsoleController.StopConsole(); });

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

        public List<Task> StartScrapper(string website, string channelPath, int minutes)
        {
            EnvironmentModel environment = EnvironmentModel.CreateEnvironment(website);
            ScrapperController scrapperController = new ScrapperController(_logger, environment, website, channelPath);
            List<Task> tasks = new List<Task>();
            tasks.Add(Task.Run(() => scrapperController.RunViewerGameScrapper(minutes)));
            tasks.Add(Task.Run(() => scrapperController.RunChatScrapper(minutes)));
            return tasks;
        }
    }
}