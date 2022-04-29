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
            ScrapperController scrapper = CreateScrapper("booyah", "pelegrino1993");
            //Task.Run(() => RunViewerScrapper(scrapper));
            Task.Run(() => RunChatScrapper(scrapper));
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="website"></param>
        /// <param name="livestreamPath"></param>
        /// <returns></returns>
        public ScrapperController CreateScrapper(string website, string livestreamPath)
        {
            //Controllers
            EnvironmentModel environment = EnvironmentModel.CreateEnvironment(website);
            ScrapperController scrapperController = new ScrapperController(_logger, environment);

            //Iniciate browser page
            scrapperController.OpenBrowserPage(livestreamPath);

            return scrapperController;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scrapperController"></param>
        /// <returns></returns>
        public Task RunViewerScrapper(ScrapperController scrapperController)
        {
            //Controllers
            TimerController timerController = new TimerController(_logger);
            FileController fileController = new FileController(_logger);

            //Variables
            int highestViewerCount = 0;

            //Loop scrapping per sec.
            Console.WriteLine($"Viewer Start time: {timerController.StartTimer()}");

            int test = 20;
            double miliseconds = 5000;

            List<string> listCounter = new List<string>();

            while (test > 0)
            {
                DateTime start = DateTime.Now;

                //Local variables
                int? counter = scrapperController.ReadViewerCounter();

                if (counter.HasValue)
                {
                    listCounter.Add(counter.Value.ToString());
                }

                //Get highest viwercount
                if (counter.HasValue && counter.Value >= highestViewerCount)
                {
                    highestViewerCount = counter.Value;
                }

                //Timer e sleep control
                TimeSpan timeSpan = DateTime.Now - start;
                if (timeSpan.TotalMilliseconds < miliseconds)
                {
                    Thread.Sleep((int)(miliseconds - timeSpan.TotalMilliseconds));
                }

                Console.WriteLine($"Viewer Lap count: {timerController.LapCount} - Lap timer: {timerController.GetTimerLap()}");
                test -= 1;
            }

            fileController.WriteToCsv("count.csv", listCounter);
            Console.WriteLine($"Viewer Stop time: {timerController.StopTimer()}");

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scrapperController"></param>
        /// <returns></returns>
        public Task RunChatScrapper(ScrapperController scrapperController)
        {
            //Controllers
            TimerController timerController = new TimerController(_logger);
            FileController fileController = new FileController(_logger);

            //Variables
            Dictionary<string, string> chatInteractions = new Dictionary<string, string>();

            //Loop scrapping per sec.
            Console.WriteLine($"\nChat Start time: {timerController.StartTimer()}");

            int test = 120;
            double miliseconds = 5000;

            while (test > 0)
            {
                DateTime start = DateTime.Now;

                //Local variables
                List<ChatMessageModel> chatMessages = scrapperController.ReadChat();

                //Get message counter for each viewer
                foreach (var author in chatMessages.Select(chatMessages => chatMessages.Author))
                {
                    if (chatInteractions.ContainsKey(author))
                    {
                        chatInteractions[author] = IncrementStringNumber(chatInteractions[author]);
                    }
                    else
                    {
                        chatInteractions.TryAdd(author, "1");
                    }
                }

                //Timer e sleep control
                TimeSpan timeSpan = DateTime.Now - start;
                if (timeSpan.TotalMilliseconds < miliseconds)
                {
                    Thread.Sleep((int)(miliseconds - timeSpan.TotalMilliseconds));
                }

                Console.WriteLine($"\nChat Lap count: {timerController.LapCount} - Lap timer: {timerController.GetTimerLap()}");
                test -= 1;
            }

            List<string> fileLines = chatInteractions.SelectMany(kvp => kvp.Value.Select(val => $"{kvp.Key} : {val}")).ToList();

            fileController.WriteToCsv("chat.csv", fileLines);
            Console.WriteLine($"\nChat Stop time: {timerController.StopTimer()}");

            return Task.CompletedTask;
        }

        public string IncrementStringNumber(string str)
        {
            string strNew = "";
            if (!string.IsNullOrEmpty(str) && int.TryParse(str, out int num))
            {
                num++;
                strNew = num.ToString();
            }
            return strNew;
        }
    }
}