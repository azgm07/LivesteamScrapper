using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;

namespace LivesteamScrapper.Controllers
{
    public class ScrapperController : Controller
    {
        private readonly ILogger<Controller> _logger;
        public bool IsScrapping { get; set; }
        public bool IsBrowserOpened { get; set; }
        public readonly EnvironmentModel _environment;
        private readonly BrowserController _browserController;
        private string lastMessage = "";
        private List<Task> Tasks;
        private System.Timers.Timer timerTask;
        private CancellationTokenSource cts;
        private bool isReloading;
        private int messagesFound;
        private int chatTimeout;

        public string Livestream { get; set; }

        //Constructor
        public ScrapperController(ILogger<Controller> logger, EnvironmentModel environment, string livestream)
        {
            _logger = logger;
            _environment = environment;
            Livestream = livestream;
            Tasks = new List<Task>();
            _browserController = new BrowserController(logger);
            timerTask = new System.Timers.Timer();
            cts = new CancellationTokenSource();
            isReloading = false;
            messagesFound = 0;
            chatTimeout = 60000;
        }

        private bool OpenScrapper()
        {
            //OpenBrowser
            try
            {
                _browserController.OpenBrowserPage(_environment.Http + Livestream, _environment.Selector);
                if (_browserController.IsReady && _browserController.Browser != null)
                {
                    PrepareScrapperPage(_environment, _browserController.Browser);
                }
                ConsoleController.ShowBrowserLog(EnumsModel.BrowserLog.Ready);
                return true;
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
                ConsoleController.ShowBrowserLog(EnumsModel.BrowserLog.NotReady);
                return false;
            }

        }

        private void ReloadScrapper()
        {
            //ReloadBrowser
            try
            {
                ConsoleController.ShowBrowserLog(EnumsModel.BrowserLog.Reloading);
                isReloading = true;
                _browserController.ReloadBrowserPage(_environment.Selector);
                if (_browserController.IsReady && _browserController.Browser != null)
                {
                    PrepareScrapperPage(_environment, _browserController.Browser);
                }
                ConsoleController.ShowBrowserLog(EnumsModel.BrowserLog.Ready);
                isReloading = false;
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
                ConsoleController.ShowBrowserLog(EnumsModel.BrowserLog.NotReady);
                isReloading = false;
            }

        }

        public void Run(int minutes)
        {
            cts = new CancellationTokenSource();

            if (OpenScrapper())
            {
                Task.Run(() => TimerTasksCancellation(minutes));

                List<Task> tasks = new List<Task>();
                tasks.Add(Task.Run(() => RunViewerGameScrapper(cts.Token)));
                tasks.Add(Task.Run(() => RunChatScrapper(cts.Token)));
                Tasks = tasks;

                //Console Tasks
                Task.Run(() => ConsoleController.StartConsole(30));
                Task.Run(() => { Task.WaitAll(tasks.ToArray()); ConsoleController.StopConsole(); });
            }

        }

        public void Stop()
        {
            cts.Cancel();
        }

        public Task TimerTasksCancellation(int minutes)
        {
            double totalMsec = minutes * 60000;

            timerTask = new System.Timers.Timer(totalMsec);
            timerTask.Elapsed += (sender, e) => Stop();
            timerTask.Start();

            return Task.CompletedTask;
        }

        public (List<ChatMessageModel>, int lastIndex) ReadChat()
        {
            //Verify if the browser is already open with a page
            if (_browserController.Browser == null)
            {
                return (new List<ChatMessageModel>(), 0);
            }

            try
            {
                //Retrive new comments
                List<ChatMessageModel> scrapeMessages = new List<ChatMessageModel>();
                var chat = _browserController.Browser.FindElement(_environment.ChatContainer);
                var messages = chat.FindElements(_environment.MessageContainer);

                //Transform all messages to a list in order
                foreach (var message in messages)
                {
                    ChatMessageModel newMessage = new ChatMessageModel();
                    string messageAuthor, messageContent;

                    try
                    {
                        messageAuthor = message.FindElement(_environment.MessageAuthor).Text;
                    }
                    catch
                    {
                        messageAuthor = "";
                    }

                    try
                    {
                        messageContent = message.FindElement(_environment.MessageContent).Text;
                    }
                    catch
                    {
                        messageContent = "";
                    }

                    if (!string.IsNullOrEmpty(messageAuthor))
                    {
                        newMessage.Author = messageAuthor;
                        newMessage.Content = messageContent;

                        scrapeMessages.Add(newMessage);
                    }
                }

                //Limits the return list based on the lastmessage found
                int lastIndex = -1;
                List<ChatMessageModel> returnMessages;

                if (!string.IsNullOrEmpty(lastMessage) && scrapeMessages.Count > 0)
                {
                    lastIndex = scrapeMessages.FindLastIndex(item => string.Concat(item.Author, ",", item.Content) == lastMessage);
                    lastMessage = string.Concat(scrapeMessages.Last().Author, ",", scrapeMessages.Last().Content);
                }
                else if (scrapeMessages.Count > 0)
                {
                    lastMessage = string.Concat(scrapeMessages.Last().Author, ",", scrapeMessages.Last().Content);
                }

                if (scrapeMessages.Count > 0 && scrapeMessages.Count - 1 != lastIndex)
                {
                    returnMessages = scrapeMessages.GetRange(lastIndex + 1, scrapeMessages.Count - (lastIndex + 1));
                }
                else
                {
                    returnMessages = new List<ChatMessageModel>();
                }

                if (!string.IsNullOrEmpty(lastMessage))
                {
                    ConsoleController.Chat.LastMessage = lastMessage;
                }

                messagesFound += returnMessages.Count;
                ConsoleController.Chat.MessagesFound = messagesFound;

                return (returnMessages, lastIndex);
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
                return (new List<ChatMessageModel>(), 0);
            }
        }

        public int? ReadViewerCounter()
        {
            //Verify if the browser is already open with a page
            if (_browserController.Browser == null)
            {
                return null;
            }

            try
            {
                //Retrive new comments
                int viewersCount = 0;
                var counter = _browserController.Browser.FindElement(_environment.CounterContainer);
                string counterText = counter.GetAttribute("textContent");

                //Treat different types of text
                if (counterText.IndexOf("mil") != -1 || counterText.IndexOf("K") != -1)
                {
                    counterText = Regex.Replace(counterText, "[^0-9,.]", "");
                    counterText = counterText.Replace(".", ",");
                    if (decimal.TryParse(counterText, out decimal result))
                    {
                        viewersCount = (int)(result * 1000);
                    }
                }
                else
                {
                    counterText = Regex.Replace(counterText, "[^0-9]", "");
                    if (int.TryParse(counterText, out int result))
                    {
                        viewersCount = result;
                    }
                }


                ConsoleController.Viewers.Count = viewersCount;
                return viewersCount;
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
                return null;
            }
        }

        public string? ReadCurrentGame()
        {
            //Verify if the browser is already open with a page
            if (_browserController.Browser == null)
            {
                return null;
            }

            try
            {
                //Retrive new comments
                var game = _browserController.Browser.FindElement(_environment.GameContainer);
                string currentGame = game.GetAttribute("textContent");

                ConsoleController.CurrentGame.Name = currentGame;
                return currentGame;
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
                return null;
            }
        }

        public Task RunViewerGameScrapper(CancellationToken token)
        {
            //Controllers
            TimeController timeController = new TimeController(_logger, "RunViewerScrapper");

            //Loop scrapping per sec.
            timeController.Start();

            double waitMilliseconds = 1000;

            List<int> listCounter = new List<int>();
            string? currentGame = null;

            //Start timer to control verify counter and submit highest
            bool flush = false;
            using (System.Timers.Timer timer = new System.Timers.Timer(60000))
            {
                timer.Elapsed += (sender, e) => flush = true;
                timer.AutoReset = true;
                timer.Start();

                //Main loop
                while (!token.IsCancellationRequested)
                {
                    DateTime start = DateTime.Now;

                    //Local variables
                    int? counter = this.ReadViewerCounter();
                    currentGame = this.ReadCurrentGame();

                    if (counter.HasValue && !string.IsNullOrEmpty(currentGame))
                    {
                        listCounter.Add(counter.Value);
                    }

                    //Each 60 sec records is transfered to the csv file
                    if (flush)
                    {
                        Task.Run(() =>
                        {
                            string max = string.Concat(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), ",", currentGame, ",", listCounter.Max().ToString());
                            List<string> newList = new List<string>();
                            newList.Add(max);
                            Write(newList, _environment.Website, this.Livestream, "counters");
                            listCounter = new List<int>();
                        });
                        timeController.Lap("Saved highest viewercount on csv");
                        flush = false;
                    }

                    //Timer e sleep control
                    TimeSpan timeSpan = DateTime.Now - start;
                    if (timeSpan.TotalMilliseconds < waitMilliseconds)
                    {
                        Thread.Sleep((int)(waitMilliseconds - timeSpan.TotalMilliseconds));
                    }

                    //Case reloading wait
                    while (isReloading)
                    {
                        Thread.Sleep(1000);
                    }
                }
            }

            //Send to file the rest of counter lines
            Task.Run(() =>
            {
                if (listCounter.Count > 0)
                {
                    string max = string.Concat(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), ",", currentGame, ",", listCounter.Max().ToString());
                    List<string> newList = new List<string>();
                    newList.Add(max);
                    Write(newList, _environment.Website, this.Livestream, "counters");
                }
            });

            timeController.Stop();

            return Task.CompletedTask;
        }

        public Task RunChatScrapper(CancellationToken token)
        {

            //Controllers
            TimeController timeController = new TimeController(_logger, "RunChatScrapper");

            //Variables
            List<ChatMessageModel> chatMessages = new List<ChatMessageModel>();
            Dictionary<string, string> chatInteractions = new Dictionary<string, string>();

            //Loop scrapping per sec.
            timeController.Start();

            double waitMilliseconds = 1000;

            //Start timer to control timeout of missing messages
            bool needRestart = false;
            int savedMessagesFound = 0;

            using (System.Timers.Timer timer = new System.Timers.Timer(chatTimeout))
            {
                timer.Elapsed += (sender, e) =>
                {
                    if (savedMessagesFound < messagesFound)
                    {
                        savedMessagesFound = messagesFound;
                        needRestart = false;
                    }
                    else
                    {
                        savedMessagesFound = messagesFound;
                        needRestart = true;
                    }
                };
                timer.AutoReset = true;
                timer.Start();

                //Main loop
                while (!token.IsCancellationRequested)
                {
                    DateTime start = DateTime.Now;

                    //Local variables
                    (List<ChatMessageModel> currentMessages, int lastIndex) = this.ReadChat();
                    chatMessages.AddRange(currentMessages);


                    if (chatMessages.Count > 0)
                    {
                        //Get message counter for each viewer
                        Task.Run(() =>
                        {
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
                        });

                        //Save all messages and reset
                        Task.Run(() =>
                        {
                            List<string> messages = new List<string>();
                            foreach (var item in chatMessages)
                            {
                                messages.Add(string.Concat(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), ",", item.Author, ",", item.Content));
                            }
                            Write(messages, _environment.Website, this.Livestream, "messages");
                            chatMessages = new List<ChatMessageModel>();
                        });
                    }

                    //Needs to reload the page case not found any new message
                    if (needRestart)
                    {
                        Task.Run(() =>
                        {
                            ReloadScrapper();
                        });
                        needRestart = false;
                    }

                    //Timer e sleep control
                    TimeSpan timeSpan = DateTime.Now - start;
                    if (timeSpan.TotalMilliseconds < waitMilliseconds)
                    {
                        Thread.Sleep((int)(waitMilliseconds - timeSpan.TotalMilliseconds));
                    }

                    //Case reloading wait
                    while (isReloading)
                    {
                        Thread.Sleep(1000);
                    }
                }
            }

            timeController.Stop();

            //Tranfere para arquivo ao final caso tenha sobrado
            if (chatMessages.Count > 0)
            {
                //Get message counter for each viewer
                Task.Run(() =>
                {
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
                });

                //Save all remaining messages
                Task.Run(() =>
                {
                    List<string> messages = new List<string>();
                    foreach (var item in chatMessages)
                    {
                        messages.Add(string.Concat(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), ",", item.Author, ",", item.Content));
                    }
                    Write(messages, _environment.Website, this.Livestream, "messages");
                });
            }

            //Process interactions
            List<string> fileLines = chatInteractions.SelectMany(kvp => kvp.Value.Select(val => $"{kvp.Key},{val}")).ToList();
            Write(fileLines, _environment.Website, this.Livestream, "viewers", true);

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

        public void Write(List<string> lines, string website, string livestream, string type, bool startNew = false)
        {
            string file = $"{GetUntilSpecial(website.ToLower())}-{GetUntilSpecial(livestream.ToLower())}-{type}.csv";

            if (startNew)
            {
                FileController.WriteCsv(file, lines);
            }
            else
            {
                FileController.UpdateCsv(file, lines);
            }
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

        //Handle page start by environment
        public void PrepareScrapperPage(EnvironmentModel environment, ChromeDriver browser)
        {
            switch (environment.Website)
            {
                case "facebook":
                    _browserController.WaitUntilElementClickable(LiveElementsModel.GetElements(environment.Website).CloseChatAnnouncement).Click();
                    chatTimeout = 5000;
                    break;
                default:
                    break;
            }
        }
    }
}