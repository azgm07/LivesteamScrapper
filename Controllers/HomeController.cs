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
            List<Task> tasks = new List<Task>();
            tasks.Add(StartScrapperAsync("booyah", "vanquilha"));
            
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

        public async Task StartScrapperAsync(string website, string channelPath)
        {
            EnvironmentModel environment = EnvironmentModel.GetEnvironment(website);
            ScrapperController scrapperController = new ScrapperController(_logger, environment, channelPath);
            await scrapperController.RunTestAsync(5);
        }
    }
}