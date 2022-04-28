using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Diagnostics;

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
            Task.Run(RunScrapper);
            return View();
        }

        public async Task RunScrapper()
        {
            //Controllers
            string chromePath = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
            ScrapperController scrapperController = new ScrapperController(_logger, chromePath);
            TimerController timerController = new TimerController(_logger);
            FileController fileController = new FileController(_logger);

            //Iniciate browser page
            string url = "https://booyah.live/velloso?source=28";
            string htmlSelector = ".message-list";
            await scrapperController.OpenBrowserPage(url, htmlSelector);
            
            //Variables
            int highestViwerCount = 0;
            ConcurrentDictionary<string, int> chatInteractions = new ConcurrentDictionary<string, int>();

            //Loop scrapping per sec.
            Console.WriteLine($"Start time: {timerController.StartTimer()}");

            int test = 60;
            while (test > 0)
            {
                //Local variables
                int? counter = await scrapperController.ReadViewerCounter();
                List<ChatMessageModel> chatMessages = await scrapperController.ReadChat();

                //Get highest viwercount
                if(counter != null && counter.Value >= highestViwerCount)
                {
                    highestViwerCount = counter.Value;
                }

                //Get message counter for each viewer
                Parallel.ForEach(chatMessages, (message) => 
                //foreach (var message in chatMessages)
                {
                    if(chatInteractions.ContainsKey(message.Author))
                    {
                        int value = chatInteractions[message.Author];
                        chatInteractions.TryUpdate(message.Author, value++, value);
                    }
                    else
                    {
                        chatInteractions.TryAdd(message.Author, 1);
                    }
                }
                );

                Thread.Sleep(5000);
                fileController.WriteToCsv(chatInteractions.Keys.ToList());
                test -= 1;

                Console.WriteLine($"Lap count: {timerController.lapCount.ToString()} - Lap timer: {timerController.GetTimerLap()}");
            }
            Console.WriteLine($"Stop time: {timerController.StopTimer()}");
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