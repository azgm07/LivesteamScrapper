using OpenQA.Selenium;
using Scrapper.Models;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using static Scrapper.Models.EnumsModel;

namespace Scrapper.Services;

public interface IScrapperChatService
{
    CancellationTokenSource Cts { get; }
    string CurrentGame { get; }
    bool IsScrapping { get; }
    string LastMessage { get; }
    string Livestream { get; }
    int MaxFails { get; }
    int MessagesFound { get; }
    ScrapperMode Mode { get; }
    int ViewersCount { get; }
    string Website { get; }

    List<ChatMessageModel> GetChatMessages();
    string GetUntilSpecial(string text);
    string IncrementStringNumber(string str);
    void PrepareScrapperPage();
    (List<ChatMessageModel>, int lastIndex) ReadChat();
    string? ReadCurrentGame();
    int? ReadViewerCounter();
    Task RunChatScrapperAsync(CancellationToken token);
    Task RunLoggingAsync(CancellationToken token, int delaySeconds = 30);
    Task RunTestAsync(EnvironmentModel environment, string livestream, int minutes);
    Task RunViewerGameScrapperAsync(CancellationToken token);
    Task<bool> RunViewerScrapperAsync(EnvironmentModel environment, string livestream);
    void StartTimerTasksCancellation(int minutes);
    void Stop();
    void WriteData(List<string> lines, string website, string livestream, string type, bool startNew = false);
}

public class ScrapperChatService : IScrapperChatService
{
    private readonly ILogger<ScrapperChatService> _logger;

    public bool IsScrapping { get; private set; }
    public EnvironmentModel Environment { get; private set; }
    public ScrapperMode Mode { get; private set; }
    public int MaxFails { get; private set; }
    public CancellationTokenSource Cts { get; private set; }


    private readonly IBrowserService _browser;
    private readonly IBrowserService _browserChat;
    private readonly ITimeService _time;
    private readonly IFileService _file;

    private string lastMessage = "";
    private System.Timers.Timer timerTask;
    private bool isReloading;
    private int chatTimeout;
    private int timeoutRestart;
    private readonly BlockingCollection<Task> mainTasks;

    public string Website { get; private set; }
    public string Livestream { get; private set; }
    public string CurrentGame { get; private set; }

    public int ViewersCount { get; private set; }
    public int MessagesFound { get; private set; }
    public string LastMessage { get; private set; }

    //Constructor
    public ScrapperChatService(ILogger<ScrapperChatService> logger, IBrowserService browser, IBrowserService browserChat, ITimeService time, IFileService file)
    {
        _logger = logger;
        Environment = new();
        Website = "";
        Livestream = "";
        CurrentGame = "";
        ViewersCount = 0;
        MessagesFound = 0;
        LastMessage = "";
        _browser = browser;
        _browserChat = browserChat;
        _time = time;
        _file = file;
        timerTask = new();
        Cts = new();
        isReloading = false;
        chatTimeout = 600000;
        timeoutRestart = 0;
        IsScrapping = false;
        MaxFails = 10;
        mainTasks = new();
    }

    private bool OpenScrapper()
    {
        //OpenBrowser
        try
        {
            _browser.StartBrowser();

            switch (Environment.Website)
            {
                case "facebook":
                case "youtube":
                    _browser.OpenBrowserPage($"{Environment.Http}{Livestream}/live", Environment.Selector);
                    break;
                default:
                    _browser.OpenBrowserPage($"{Environment.Http}{Livestream}", Environment.Selector);
                    break;
            }

            if (_browser.IsReady && _browser.Browser != null)
            {
                try
                {
                    PrepareScrapperPage();
                    _logger.LogInformation("Browser page is ready for {website}/{livestream}", Website, Livestream);
                    IsScrapping = true;
                }
                catch (Exception e)
                {
                    IsScrapping = false;
                    _logger.LogError( e, "OpenScrapper");
                }
            }
            else
            {
                IsScrapping = false;
            }
            return IsScrapping;
        }
        catch (Exception e)
        {
            _logger.LogError( e, "OpenScrapper");
            _logger.LogInformation("Browser page is not ready for {website}/{livestream}", Website, Livestream);
            IsScrapping = false;
            return IsScrapping;
        }

    }

    private void ReloadScrapper()
    {
        //ReloadBrowser
        try
        {
            isReloading = true;

            if (_browser.Browser == null)
            {
                _browser.StartBrowser();
            }

            _browser.ReloadBrowserPage(Environment.Selector);
            if (_browser.IsReady && _browser.Browser != null)
            {
                PrepareScrapperPage();
            }

            isReloading = false;
        }
        catch (Exception e)
        {
            _logger.LogError( e, "ReloadScrapper");
            _logger.LogInformation("Browser page is not ready for {website}/{livestream}", Website, Livestream);
            isReloading = false;
        }

    }

    public async Task RunLoggingAsync(CancellationToken token, int delaySeconds = 30)
    {
        _logger.LogInformation("Console Started for {website}/{livestream}", Website, Livestream);
        Thread.Sleep(delaySeconds * 1000);
        while (!token.IsCancellationRequested)
        {
            StringBuilder sb = new();
            sb.Append($"Stream: {Website}/{Livestream} | ");
            sb.Append($"Playing: {CurrentGame} | ");
            sb.Append($"Viewers Count: {ViewersCount} | ");
            sb.Append($"Messages found in page: {MessagesFound} | ");
            sb.Append($"Last message found: {LastMessage}");

            _logger.LogInformation("{message}", sb.ToString());

            await Task.Delay(delaySeconds * 1000).ConfigureAwait(false);
        }

        _logger.LogInformation("Console Stopped for {website}/{livestream}", Website, Livestream);
    }

    public async Task RunTestAsync(EnvironmentModel environment, string livestream, int minutes)
    {
        Environment = environment;
        Website = GetUntilSpecial(Environment.Website);
        Livestream = livestream;
        bool hasStarted = await Task.Run(() => Start());
        if (hasStarted)
        {
            StartTimerTasksCancellation(minutes);

            List<Task> tasks = new();
            tasks.Add(RunViewerGameScrapperAsync(Cts.Token));
            tasks.Add(RunChatScrapperAsync(Cts.Token));

            //Console Tasks
            tasks.Add(RunLoggingAsync(Cts.Token, 30));

            foreach (var item in tasks)
            {
                mainTasks.Add(item);
            }

            await Task.WhenAll(tasks);
        }

    }
    public async Task<bool> RunViewerScrapperAsync(EnvironmentModel environment, string livestream)
    {
        if (!IsScrapping)
        {
            Environment = environment;
            Website = GetUntilSpecial(Environment.Website);
            Livestream = livestream;

            bool hasStarted = await Task.Run(() => Start());
            int count = 3;

            while (!hasStarted && count > 0)
            {
                await Task.Delay(15000);
                count--;
                hasStarted = await Task.Run(() => Start());
            }

            if (hasStarted)
            {
                List<Task> tasks = new();
                tasks.Add(RunViewerGameScrapperAsync(Cts.Token));

                //Console Tasks
                tasks.Add(RunLoggingAsync(Cts.Token, 30));

                foreach (var item in tasks)
                {
                    mainTasks.Add(item);
                }

                _ = Task.WhenAll(tasks).ContinueWith((task) =>
                {
                    Stop();
                }, TaskScheduler.Default);
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            _logger.LogInformation("Scrapper is already running for {website}/{livestream}", Website, Livestream);
            return true;
        }
    }

    private bool Start()
    {
        Cts = new CancellationTokenSource();
        bool isOpen = OpenScrapper();

        if (isOpen)
        {
            _logger.LogInformation("Scrapper has started for {website}/{livestream}", Website, Livestream);
        }
        else
        {
            _logger.LogInformation("Scrapper failed to start for {website}/{livestream}", Website, Livestream);
        }
        return isOpen;
    }

    public void Stop()
    {
        Cts.Cancel();
        IsScrapping = false;

        if (_browser != null)
        {
            _browser.StopBrowserPage();
        }

        for (int i = 0; i < mainTasks.Count; i++)
        {
            _ = mainTasks.Take();
        }

        if (IsScrapping)
        {
            _logger.LogInformation("Scrapper has stopped for {website}/{livestream}", Website, Livestream);
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
        if (_browser == null || _browser.Browser == null)
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
                LastMessage = lastMessage;
            }

            MessagesFound += returnMessages.Count;

            return (returnMessages, lastIndex);
        }
        catch (Exception e)
        {
            _logger.LogError( e, "ReadChat");
            return (new List<ChatMessageModel>(), -1);
        }
    }

    public int? ReadViewerCounter()
    {
        //Verify if the browser is already open with a page
        if (_browser == null || _browser.Browser == null)
        {
            return null;
        }

        try
        {
            //Retrive new comments
            int viewersCount = 0;
            var counter = _browser.Browser.FindElement(Environment.CounterContainer);
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


            ViewersCount = viewersCount;
            return viewersCount;
        }
        catch (Exception e)
        {
            _logger.LogError( e, "ReadViewerCounter");
            return null;
        }
    }

    public string? ReadCurrentGame()
    {
        //Verify if the browser is already open with a page
        if (_browser == null || _browser.Browser == null)
        {
            return null;
        }

        try
        {
            //Retrive new comments
            var game = _browser.Browser.FindElement(Environment.GameContainer);
            string currentGame = game.GetAttribute("textContent");

            CurrentGame = currentGame;
            return currentGame;
        }
        catch (Exception e)
        {
            _logger.LogError( e, "ReadCurrentGame");
            return null;
        }
    }

    public async Task RunViewerGameScrapperAsync(CancellationToken token)
    {
        //Loop scrapping per sec.
        _time.Start("RunViewerScrapper");

        //Flush tasks
        List<Task> tasksFlush = new();

        //Reload tasks
        List<Task> tasksReload = new();

        double waitMilliseconds = 1000;

        List<int> listCounter = new();
        string? currentGame = null;
        int? counter = null;

        //Failed atempts
        int failedAtempts = 0;

        //Start timer to control verify counter and submit highest
        bool needRestart = false;
        bool flush = false;

        using System.Timers.Timer timer = new(60000);
        timer.Elapsed += (sender, e) => flush = true;
        timer.AutoReset = true;
        timer.Start();

        System.Timers.Timer timerRestart = new();
        if (timeoutRestart > 0)
        {
            timerRestart = new(timeoutRestart);
            timerRestart.Elapsed += (sender, e) =>
            {
                needRestart = true;
            };
            timerRestart.AutoReset = true;
            timerRestart.Start();
        }

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
                _logger.LogInformation("Scrapper failed and has to stop for {website}/{livestream}", Website, Livestream);
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
                    WriteData(newList, Environment.Website, Livestream, "counters");
                    listCounter = new List<int>();
                }, CancellationToken.None);

                flush = false;

                tasksFlush.Add(task);
            }

            //Timer e sleep control
            TimeSpan timeSpan = DateTime.Now - start;
            if (timeSpan.TotalMilliseconds < waitMilliseconds)
            {
                await Task.Delay((int)(waitMilliseconds - timeSpan.TotalMilliseconds), CancellationToken.None);
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

            //Case reloading wait
            while (isReloading)
            {
                await Task.Delay(1000, CancellationToken.None);
            }

        }

        //Stop timers
        _time.Stop();
        timer.Stop();
        timerRestart.Stop();

        //Send to file the rest of counter lines
        if (listCounter.Count > 0)
        {
            await Task.Run(() =>
            {
                string max = string.Concat(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), ",", currentGame, ",", listCounter.Max().ToString());
                List<string> newList = new();
                newList.Add(max);
                WriteData(newList, Environment.Website, Livestream, "counters");
            }, CancellationToken.None);
        }

        await Task.WhenAll(tasksFlush);

    }

    public async Task RunChatScrapperAsync(CancellationToken token)
    {
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
        _time.Start("RunChatScrapper");

        double waitMilliseconds = 1000;

        //Start timer to control timeout of missing messages
        bool needRestart = false;
        int savedMessagesFound = 0;

        using System.Timers.Timer timer = new(chatTimeout);

        timer.Elapsed += (sender, e) =>
        {
            if (savedMessagesFound < MessagesFound)
            {
                savedMessagesFound = MessagesFound;
                needRestart = false;
            }
            else
            {
                savedMessagesFound = MessagesFound;
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
                _logger.LogInformation("Scrapper failed and has to stop for {website}/{livestream}", Website, Livestream);
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
                    WriteData(messages, Environment.Website, Livestream, "messages");
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

        //Stop timers
        _time.Stop();
        timer.Stop();

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
                WriteData(messages, Environment.Website, Livestream, "messages");
            }, CancellationToken.None);
            tasksFlush.Add(taskMessages);
        }

        await Task.WhenAll(tasksFlush);

        //Chat interactions
        await Task.Run(() =>
        {
            List<string> fileLines = chatInteractions.SelectMany(kvp => kvp.Value.Select(val => $"{kvp.Key},{val}")).ToList();
            WriteData(fileLines, Environment.Website, Livestream, "viewers", true);
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

    public void WriteData(List<string> lines, string website, string livestream, string type, bool startNew = false)
    {
        string file = $"{GetUntilSpecial(website.ToLower())}-{GetUntilSpecial(livestream.ToLower())}-{type}.csv";

        _file.WriteCsv("files/csv", file, lines, startNew);
    }

    public string GetUntilSpecial(string text)
    {
        //Get until a special character appear
        StringBuilder sb = new();
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] >= '0' && text[i] <= '9' || text[i] >= 'A' && text[i] <= 'Z' || text[i] >= 'a' && text[i] <= 'z' || text[i] == '.' || text[i] == '_')
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
        switch (Environment.Website)
        {
            case "facebook":
                chatTimeout = 5000;
                timeoutRestart = 10000;

                if (_browser.Browser != null)
                {
                    _browser.WaitUntilElementClickable(LiveElementsModel.GetElements(Environment.Website).OpenLive).Click();
                    _browser.WaitUntilElementClickable(LiveElementsModel.GetElements(Environment.Website).CloseChatAnnouncement).Click();
                }
                break;
            case "youtube":
                //Open chat in aux browser page
                if (_browserChat.Browser == null)
                {
                    _browserChat.StartBrowser(false);
                }
                else
                {
                    _browserChat.ReloadBrowserPage();
                }
                url = Environment.Http + "live_chat?is_popout=1&v=" + Livestream.Split("=")[1];
                _browserChat.OpenBrowserPage(url, null);

                //Change the name to channel name
                if (_browser.Browser != null)
                {
                    try
                    {
                        string name = _browser.Browser.FindElements(LiveElementsModel.GetElements(Environment.Website).ChannelName)[0].Text;
                        Livestream = name;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError( e, "PrepareScrapperPage");
                    }
                }
                break;
            case "twitch":
                //Open chat in aux browser page
                if (_browserChat.Browser == null)
                {
                    _browserChat.StartBrowser(false);
                }
                url = Environment.Http + "popout/" + Livestream + "/chat?popout=";
                _browserChat.OpenBrowserPage(url, null);
                break;
            default:
                break;
        }
    }

    //Handle chat scrapper by environment
    public List<ChatMessageModel> GetChatMessages()
    {
        ReadOnlyCollection<IWebElement> messages = new(new List<IWebElement>());
        List<ChatMessageModel> scrapeMessages = new();

        if (_browser == null || _browser.Browser == null)
        {
            return new List<ChatMessageModel>();
        }

        try
        {
            switch (Environment.Website)
            {
                case "youtube":
                case "twitch":
                    //To Iframe
                    if (_browserChat.Browser == null)
                    {
                        _browserChat.StartBrowser(false);
                    }

                    if (_browserChat.Browser != null)
                    {
                        messages = _browserChat.Browser.FindElements(Environment.MessageContainer);

                    }
                    break;
                default:
                    if (_browserChat.Browser != null)
                    {
                        var chat = _browser.Browser.FindElement(Environment.ChatContainer);
                        messages = chat.FindElements(Environment.MessageContainer);
                    }
                    break;
            }

            //Transform all messages to a list in order
            foreach (var message in messages)
            {
                ChatMessageModel newMessage = new();
                string messageAuthor, messageContent;

                try
                {
                    messageAuthor = message.FindElement(Environment.MessageAuthor).Text;
                }
                catch
                {
                    messageAuthor = "";
                }

                try
                {
                    messageContent = message.FindElement(Environment.MessageContent).Text;
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
            _logger.LogError( e, "GetChatMessages");
        }

        return scrapeMessages;
    }
}
