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
            //Start tasks
            List<Task> tasks = StartScrapper("booyah", "vanquilha", 1);

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

        public List<Task> StartScrapper(string site, string channel, int minutes)
        {
            ScrapperController scrapper = CreateScrapper(site, channel);
            List<Task> tasks = new List<Task>();
            tasks.Add(Task.Run(() => RunViewerGameScrapper(scrapper, minutes)));
            tasks.Add(Task.Run(() => RunChatScrapper(scrapper, minutes)));
            return tasks;
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
        public Task RunViewerGameScrapper(ScrapperController scrapperController, int timeInMinutes)
        {
            //Controllers
            TimerController timerController = new TimerController(_logger, "RunViewerScrapper");
            FileController fileController = new FileController(_logger);

            //Variables
            int highestViewerCount = 0;

            //Loop scrapping per sec.
            timerController.StartTimer();

            double totalMsec = timeInMinutes * 60000;
            double waitMilliseconds = 5000;

            List<string> listCounter = new List<string>();

            while (totalMsec > 0)
            {
                DateTime start = DateTime.Now;

                //Local variables
                int? counter = scrapperController.ReadViewerCounter();
                string? currentGame = scrapperController.ReadCurrentGame();

                if (counter.HasValue && !string.IsNullOrEmpty(currentGame))
                {
                    listCounter.Add(string.Concat(currentGame, " - ", counter.Value.ToString()));
                }

                //Get highest viwercount
                if (counter.HasValue && counter.Value >= highestViewerCount)
                {
                    highestViewerCount = counter.Value;
                }

                //Timer e sleep control
                TimeSpan timeSpan = DateTime.Now - start;
                if (timeSpan.TotalMilliseconds < waitMilliseconds)
                {
                    Thread.Sleep((int)(waitMilliseconds - timeSpan.TotalMilliseconds));
                }

                totalMsec -= (DateTime.Now - start).TotalMilliseconds;
            }

            fileController.WriteToCsv("count.csv", listCounter);
            timerController.StopTimer();

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scrapperController"></param>
        /// <returns></returns>
        public Task RunChatScrapper(ScrapperController scrapperController, int timeInMinutes)
        {
            //Controllers
            TimerController timerController = new TimerController(_logger, "RunChatScrapper");
            FileController fileController = new FileController(_logger);

            //Variables
            Dictionary<string, string> chatInteractions = new Dictionary<string, string>();

            //Loop scrapping per sec.
            timerController.StartTimer();

            double totalMsec = timeInMinutes * 60000;
            double waitMilliseconds = 5000;

            while (totalMsec > 0)
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
                if (timeSpan.TotalMilliseconds < waitMilliseconds)
                {
                    Thread.Sleep((int)(waitMilliseconds - timeSpan.TotalMilliseconds));
                }

                totalMsec -= (DateTime.Now - start).TotalMilliseconds;
            }

            List<string> fileLines = chatInteractions.SelectMany(kvp => kvp.Value.Select(val => $"{kvp.Key} : {val}")).ToList();

            fileController.WriteToCsv("chat.csv", fileLines);
            timerController.StopTimer();

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