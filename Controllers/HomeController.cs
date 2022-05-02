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
            ScrapperController scrapperController = new ScrapperController(_logger, environment, website, livestreamPath);

            //Iniciate browser page
            scrapperController.OpenBrowserPage();

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

            WriteCounters(listCounter, scrapperController.Website, scrapperController.Livestream);
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

            //Variables
            List<ChatMessageModel> chatMessages = new List<ChatMessageModel>();

            //Loop scrapping per sec.
            timerController.StartTimer();

            double totalMsec = timeInMinutes * 60000;
            double waitMilliseconds = 5000;

            while (totalMsec > 0)
            {
                DateTime start = DateTime.Now;

                //Local variables
                (List<ChatMessageModel> currentMessages, int lastIndex) = scrapperController.ReadChat();
                chatMessages.AddRange(currentMessages);

                //Timer e sleep control
                TimeSpan timeSpan = DateTime.Now - start;
                if (timeSpan.TotalMilliseconds < waitMilliseconds)
                {
                    Thread.Sleep((int)(waitMilliseconds - timeSpan.TotalMilliseconds));
                }

                totalMsec -= (DateTime.Now - start).TotalMilliseconds;
            }
                        
            timerController.StopTimer();

            WriteChat(chatMessages, scrapperController.Website, scrapperController.Livestream);

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

        public void WriteChat(List<ChatMessageModel> chatMessages, string website, string livestream)
        {
            //Variables
            Dictionary<string, string> chatInteractions = new Dictionary<string, string>();

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

            List<string> fileLines = chatInteractions.SelectMany(kvp => kvp.Value.Select(val => $"{kvp.Key} : {val}")).ToList();

            string file = $"{GetUntilSpecial(website.ToLower())}-{GetUntilSpecial(livestream.ToLower())}-chat.csv";

            FileController.WriteToCsv(file, fileLines);
        }

        public void WriteCounters(List<string> counters, string website, string livestream)
        {
            string file = $"{GetUntilSpecial(website.ToLower())}-{GetUntilSpecial(livestream.ToLower())}-counters.csv";

            FileController.WriteToCsv(file, counters);
        }

        public string GetUntilSpecial(string text)
        {
            //Get until a special character appear
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                if ((text[i] >= '0' && text[i] <= '9') || (text[i] >= 'A' && text[i] <= 'Z') || (text[i] >= 'a' && text[i] <= 'z') || text[i] == '.' || text[i] == '_')
                {
                    sb.Append(text[i]);
                }
                else
                {
                    break;
                }
            }
            return sb.ToString();
        }
    }
}