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
            StartScrapper("twitch", "zigueira", 5);
            
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

        public void StartScrapper(string website, string channelPath, int minutes)
        {
            EnvironmentModel environment = EnvironmentModel.GetEnvironment(website);
            ScrapperController scrapperController = new ScrapperController(_logger, environment, channelPath);
            scrapperController.Run(minutes);
        }
    }
}