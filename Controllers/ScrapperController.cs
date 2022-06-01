using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using static LivesteamScrapper.Models.EnumsModel;

namespace LivesteamScrapper.Controllers
{
    public sealed class ScrapperController : Controller, IDisposable
    {
        private readonly ILogger<Controller> _logger;

        public bool IsScrapping { get; private set; }
        public readonly EnvironmentModel _environment;
        public ScrapperMode Mode { get; private set; }
        public int MaxFails { get; set; }


        private readonly BrowserController _browserController;
        private BrowserController? _browserControllerChat;
        private readonly ConsoleController consoleController;

        private string lastMessage = "";
        private System.Timers.Timer timerTask;
        private CancellationTokenSource cts;
        private bool isReloading;
        private int messagesFound;
        private int chatTimeout;
        private readonly List<Task> mainTasks;

        public string Livestream { get; set; }

        //Constructor
        public ScrapperController(ILogger<Controller> logger, EnvironmentModel environment, string livestream)
        {
            _logger = logger;
            _environment = environment;
            Livestream = livestream;
            _browserController = new(logger);
            _browserControllerChat = null;
            consoleController = new();
            timerTask = new();
            cts = new();
            isReloading = false;
            messagesFound = 0;
            chatTimeout = 600000;
            IsScrapping = false;
            Mode = ScrapperMode.Off;
            MaxFails = 10;
            mainTasks = new();

            //Setup Console
            consoleController.Channel.Website = GetUntilSpecial(environment.Website.ToLower());
            consoleController.Channel.Name = GetUntilSpecial(this.Livestream.ToLower());
        }

        public new void Dispose()
        {
            Stop();
            if (_browserController != null)
            {
                _browserController.Dispose();
            }
            if (_browserControllerChat != null)
            {
                _browserControllerChat.Dispose();
            }
            timerTask.Dispose();
            cts.Dispose();
        }

        private bool OpenScrapper()
        {
            //OpenBrowser
            try
            {
                _browserController.OpenBrowserPage(_environment.Http + Livestream, _environment.Selector);
                if (_browserController.IsReady && _browserController.Browser != null)
                {
                    try
                    {
                        PrepareScrapperPage();
                        ConsoleController.ShowBrowserLog(EnumsModel.BrowserLog.Ready);
                        IsScrapping = true;
                    }
                    catch (Exception e)
                    {
                        IsScrapping = false;
                        ConsoleController.ShowExceptionLog(e.Message);
                    }
                }
                else
                {
                    IsScrapping = false;
                }
                return true;
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
                ConsoleController.ShowBrowserLog(EnumsModel.BrowserLog.NotReady);
                IsScrapping = false;
                return false;
            }

        }

        private void ReloadScrapper()
        {
            //ReloadBrowser
            try
            {
                if (chatTimeout > 10)
                {
                    ConsoleController.ShowBrowserLog(EnumsModel.BrowserLog.Reloading);
                }

                isReloading = true;

                _browserController.ReloadBrowserPage(_environment.Selector);
                if (_browserController.IsReady && _browserController.Browser != null)
                {
                    PrepareScrapperPage();
                }

                if (chatTimeout > 10)
                {
                    ConsoleController.ShowBrowserLog(EnumsModel.BrowserLog.Ready);
                }
                isReloading = false;
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
                ConsoleController.ShowBrowserLog(EnumsModel.BrowserLog.NotReady);
                isReloading = false;
            }

        }

        public async Task RunTestAsync(int minutes)
        {
            bool hasStarted = await Task.Run(() => Start());
            if (hasStarted)
            {
                Mode = ScrapperMode.All;
                StartTimerTasksCancellation(minutes);

                List<Task> tasks = new();
                tasks.Add(RunViewerGameScrapperAsync(cts.Token));
                tasks.Add(RunChatScrapperAsync(cts.Token));

                //Console Tasks
                tasks.Add(consoleController.RunConsoleAsync(cts.Token, 30));
                mainTasks.AddRange(tasks);
                await Task.WhenAll(tasks);
            }

        }
        public async Task RunViewerScrapperAsync()
        {
            if (!IsScrapping)
            {
                bool hasStarted = await Task.Run(() => Start());
                int count = 4;

                while (!hasStarted && count > 0)
                {
                    hasStarted = await Task.Run(() => Start());
                    count--;
                    await Task.Delay(15000);
                }

                if (hasStarted)
                {
                    Mode = ScrapperMode.Viewers;
                    List<Task> tasks = new();
                    tasks.Add(RunViewerGameScrapperAsync(cts.Token));

                    //Console Tasks
                    tasks.Add(consoleController.RunConsoleAsync(cts.Token, 30));

                    mainTasks.AddRange(tasks);
                    await Task.WhenAll(tasks);
                    await Task.Run(() => Stop());
                }
            }
            else
            {
                ConsoleController.ShowScrapperLog(ScrapperLog.Running);
            }
        }

        private bool Start()
        {
            cts = new CancellationTokenSource();
            bool isOpen = OpenScrapper();

            if (isOpen)
            {
                ConsoleController.ShowScrapperLog(ScrapperLog.Started);
            }
            else
            {
                ConsoleController.ShowScrapperLog(ScrapperLog.FailedToStart);
            }
            return isOpen;
        }

        public void Stop()
        {
            if (IsScrapping)
            {
                ConsoleController.ShowScrapperLog(ScrapperLog.Stopped);
            }
            cts.Cancel();
            IsScrapping = false;
            Mode = ScrapperMode.Off;
            foreach (Task task in mainTasks)
            {
                mainTasks.Remove(task);
            }
        }

        public void StartTimerTasksCancellation(int minutes)
        {
            double totalMsec = minutes * 60000;
            timerTask = new System.Timers.Timer(totalMsec);
            timerTask.Elapsed += (sender, e) =>
            {
                Stop();
                timerTask.Stop();
            };
            timerTask.Start();
        }

        public (List<ChatMessageModel>, int lastIndex) ReadChat()
        {
            //Verify if the browser is already open with a page
            if (_browserController.Browser == null)
            {
                return (new List<ChatMessageModel>(), -1);
            }

            try
            {
                //Retrive new comments
                List<ChatMessageModel> scrapeMessages = GetChatMessages();

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
                    consoleController.Chat.LastMessage = lastMessage;
                }

                messagesFound += returnMessages.Count;
                consoleController.Chat.MessagesFound = messagesFound;

                return (returnMessages, lastIndex);
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
                return (new List<ChatMessageModel>(), -1);
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


                consoleController.Viewers.Count = viewersCount;
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

                consoleController.CurrentGame.Name = currentGame;
                return currentGame;
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
                return null;
            }
        }

        public async Task RunViewerGameScrapperAsync(CancellationToken token)
        {
            //Controllers
            TimeController timeController = new(_logger, "RunViewerScrapper");

            //Loop scrapping per sec.
            timeController.Start();

            //Flush tasks
            List<Task> tasksFlush = new();

            double waitMilliseconds = 1000;

            List<int> listCounter = new();
            string? currentGame = null;
            int? counter = null;

            //Failed atempts
            int failedAtempts = 0;

            //Start timer to control verify counter and submit highest
            bool flush = false;
            using (System.Timers.Timer timer = new(60000))
            {
                timer.Elapsed += (sender, e) => flush = true;
                timer.AutoReset = true;
                timer.Start();

                //Main loop
                while (!token.IsCancellationRequested)
                {
                    DateTime start = DateTime.Now;

                    //Local variables
                    counter = await Task.Run(() => ReadViewerCounter(), CancellationToken.None);
                    currentGame = await Task.Run(() => ReadCurrentGame(), CancellationToken.None);

                    if (counter.HasValue && counter > 0 && !string.IsNullOrEmpty(currentGame))
                    {
                        listCounter.Add(counter.Value);
                        failedAtempts = 0;
                    }
                    else
                    {
                        failedAtempts++;
                    }

                    //Break if too many fails
                    if (failedAtempts >= MaxFails)
                    {
                        ConsoleController.ShowScrapperLog(ScrapperLog.Failed);
                        Stop();
                        break;
                    }

                    //Each 60 sec records is transfered to the csv file
                    if (flush)
                    {
                        Task task = Task.Run(() =>
                        {
                            string max = string.Concat(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), ",", currentGame, ",", listCounter.Max().ToString());
                            List<string> newList = new();
                            newList.Add(max);
                            WriteCSV(newList, _environment.Website, this.Livestream, "counters");
                            listCounter = new List<int>();
                        }, CancellationToken.None);
                        timeController.Lap("Saved highest viewercount on csv");
                        flush = false;

                        tasksFlush.Add(task);
                    }

                    //Timer e sleep control
                    TimeSpan timeSpan = DateTime.Now - start;
                    if (timeSpan.TotalMilliseconds < waitMilliseconds)
                    {
                        await Task.Delay((int)(waitMilliseconds - timeSpan.TotalMilliseconds), CancellationToken.None);
                    }

                    //Case reloading wait
                    while (isReloading)
                    {
                        await Task.Delay(1000, CancellationToken.None);
                    }
                }
            }
            timeController.Stop();

            //Send to file the rest of counter lines
            if (listCounter.Count > 0)
            {
                await Task.Run(() =>
                {
                    string max = string.Concat(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), ",", currentGame, ",", listCounter.Max().ToString());
                    List<string> newList = new();
                    newList.Add(max);
                    WriteCSV(newList, _environment.Website, this.Livestream, "counters");
                }, CancellationToken.None);
            }

            await Task.WhenAll(tasksFlush);

        }

        public async Task RunChatScrapperAsync(CancellationToken token)
        {

            //Controllers
            TimeController timeController = new(_logger, "RunChatScrapper");

            //Variables
            List<ChatMessageModel> chatMessages = new();
            Dictionary<string, string> chatInteractions = new();

            //Failed atempts
            int failedAtempts = 0;

            //Flush tasks
            List<Task> tasksFlush = new();

            //Reload tasks
            List<Task> tasksReload = new();

            //Loop scrapping per sec.
            timeController.Start();

            double waitMilliseconds = 1000;

            //Start timer to control timeout of missing messages
            bool needRestart = false;
            int savedMessagesFound = 0;

            using (System.Timers.Timer timer = new(chatTimeout))
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
                    (List<ChatMessageModel> currentMessages, int lastIndex) = await Task.Run(() => ReadChat(), CancellationToken.None);

                    if (lastIndex < 0)
                    {
                        failedAtempts++;
                    }
                    else
                    {
                        failedAtempts = 0;
                    }

                    //Break if too many fails
                    if (failedAtempts >= MaxFails)
                    {
                        ConsoleController.ShowScrapperLog(ScrapperLog.Failed);
                        Stop();
                        break;
                    }

                    if (currentMessages.Count > 0)
                    {
                        chatMessages.AddRange(currentMessages);

                        //Get message counter for each viewer
                        Task taskCounters = Task.Run(() =>
                        {
                            foreach (var author in chatMessages.Select(message => message.Author))
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
                        }, CancellationToken.None);
                        tasksFlush.Add(taskCounters);

                        //Save all messages and reset
                        Task taskMessages = Task.Run(() =>
                        {
                            List<string> messages = new();
                            foreach (var item in chatMessages)
                            {
                                messages.Add(string.Concat(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), ",", item.Author, ",", item.Content));
                            }
                            WriteCSV(messages, _environment.Website, this.Livestream, "messages");
                            chatMessages = new List<ChatMessageModel>();
                        }, CancellationToken.None);
                        tasksFlush.Add(taskMessages);
                    }

                    //Needs to reload the page case not found any new message
                    if (needRestart)
                    {
                        Task taskRestart = Task.Run(() =>
                        {
                            ReloadScrapper();
                        }, CancellationToken.None);

                        needRestart = false;
                        tasksReload.Add(taskRestart);
                        await Task.WhenAll(tasksReload);
                    }

                    //Timer e sleep control
                    TimeSpan timeSpan = DateTime.Now - start;
                    if (timeSpan.TotalMilliseconds < waitMilliseconds)
                    {
                        await Task.Delay((int)(waitMilliseconds - timeSpan.TotalMilliseconds), CancellationToken.None);
                    }

                    //Case reloading wait
                    while (isReloading)
                    {
                        await Task.Delay(1000, CancellationToken.None);
                    }
                }
            }

            timeController.Stop();

            //Send to file the rest of counter lines
            if (chatMessages.Count > 0)
            {
                //Get message counter for each viewer
                Task taskCounters = Task.Run(() =>
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
                }, CancellationToken.None);
                tasksFlush.Add(taskCounters);

                //Save all remaining messages
                Task taskMessages = Task.Run(() =>
                {
                    List<string> messages = new();
                    foreach (var item in chatMessages)
                    {
                        messages.Add(string.Concat(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), ",", item.Author, ",", item.Content));
                    }
                    WriteCSV(messages, _environment.Website, this.Livestream, "messages");
                }, CancellationToken.None);
                tasksFlush.Add(taskMessages);
            }

            await Task.WhenAll(tasksFlush);

            //Chat interactions
            await Task.Run(() =>
            {
                List<string> fileLines = chatInteractions.SelectMany(kvp => kvp.Value.Select(val => $"{kvp.Key},{val}")).ToList();
                WriteCSV(fileLines, _environment.Website, this.Livestream, "viewers", true);
            }, CancellationToken.None);
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

        public void WriteCSV(List<string> lines, string website, string livestream, string type, bool startNew = false)
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
            StringBuilder sb = new();
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
        public void PrepareScrapperPage()
        {
            string url;
            switch (_environment.Website)
            {
                case "facebook":
                    chatTimeout = 5000;
                    _browserController.WaitUntilElementClickable(LiveElementsModel.GetElements(_environment.Website).CloseChatAnnouncement).Click();
                    break;
                case "youtube":
                    //Open chat in aux browser page
                    if (_browserControllerChat == null)
                    {
                        _browserControllerChat = new BrowserController(_logger, false);
                    }
                    else
                    {
                        _browserControllerChat.ReloadBrowserPage();
                    }
                    url = _environment.Http + "live_chat?is_popout=1&v=" + Livestream.Split("=")[1];
                    _browserControllerChat.OpenBrowserPage(url, null);

                    //Change the name to channel name
                    if (_browserController.Browser != null)
                    {
                        try
                        {
                            string name = _browserController.Browser.FindElements(LiveElementsModel.GetElements(_environment.Website).ChannelName)[0].Text;
                            Livestream = name;
                        }
                        catch (Exception e)
                        {
                            ConsoleController.ShowExceptionLog(e.Message);
                        }
                    }
                    break;
                case "twitch":
                    //Open chat in aux browser page
                    if (_browserControllerChat == null)
                    {
                        _browserControllerChat = new BrowserController(_logger, false);
                    }
                    url = _environment.Http + "popout/" + Livestream + "/chat?popout=";
                    _browserControllerChat.OpenBrowserPage(url, null);
                    break;
                default:
                    break;
            }
        }

        //Handle chat scrapper by environment
        public List<ChatMessageModel> GetChatMessages()
        {
            ReadOnlyCollection<IWebElement> messages;
            List<ChatMessageModel> scrapeMessages = new();

            if (_browserController.Browser == null)
            {
                return new List<ChatMessageModel>();
            }


            ChromeDriver browserAux;

            try
            {
                switch (_environment.Website)
                {
                    case "youtube":
                    case "twitch":
                        //To Iframe
                        if (_browserControllerChat == null)
                        {
                            _browserControllerChat = new BrowserController(_logger, false);
                        }
                        browserAux = _browserControllerChat.Browser;
                        messages = browserAux.FindElements(_environment.MessageContainer);
                        break;
                    default:
                        var chat = _browserController.Browser.FindElement(_environment.ChatContainer);
                        messages = chat.FindElements(_environment.MessageContainer);
                        break;
                }

                //Transform all messages to a list in order
                foreach (var message in messages)
                {
                    ChatMessageModel newMessage = new();
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
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
            }

            return scrapeMessages;
        }
    }
}