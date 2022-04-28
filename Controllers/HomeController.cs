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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public Task RunScrapper()
        {
            //Controllers
            ScrapperController scrapperController = new ScrapperController(_logger);
            TimerController timerController = new TimerController(_logger);
            FileController fileController = new FileController(_logger);

            //Iniciate browser page
            string url = "https://booyah.live/vanquilha";
            string htmlSelector = "message-list";
            scrapperController.OpenBrowserPage(url, htmlSelector);

            //Variables
            int highestViwerCount = 0;
            ConcurrentDictionary<string, int> chatInteractions = new ConcurrentDictionary<string, int>();

            //Loop scrapping per sec.
            Console.WriteLine($"Start time: {timerController.StartTimer()}");

            int test = 60;
            int miliseconds = 5000;
            while (test > 0)
            {
                DateTime start = DateTime.Now;

                //Local variables
                int? counter = scrapperController.ReadViewerCounter();
                List<ChatMessageModel> chatMessages = scrapperController.ReadChat();

                //Get highest viwercount
                if (counter != null && counter.Value >= highestViwerCount)
                {
                    highestViwerCount = counter.Value;
                }

                //Get message counter for each viewer
                Parallel.ForEach(chatMessages, (message) =>
                {
                    if (chatInteractions.ContainsKey(message.Author))
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

                TimeSpan timeSpan = DateTime.Now - start;
                if (timeSpan.Milliseconds < miliseconds)
                {
                    Thread.Sleep(miliseconds - timeSpan.Milliseconds);
                }

                Console.WriteLine($"Lap count: {timerController.LapCount} - Lap timer: {timerController.GetTimerLap()}");
                test -= 1;
            }

            fileController.WriteToCsv(chatInteractions.Keys.ToList());
            _ = chatInteractions;
            Console.WriteLine($"Stop time: {timerController.StopTimer()}");

            return Task.CompletedTask;
        }
    }
}