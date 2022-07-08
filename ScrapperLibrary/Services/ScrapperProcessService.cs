using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using ScrapperLibrary.Models;
using ScrapperLibrary.Utils;
using System.Text;
using System.Text.RegularExpressions;
using static ScrapperLibrary.Models.Enums;
using ScrapperLibrary.Interfaces;

namespace ScrapperLibrary.Services;

public sealed class ScrapperProcessService : IScrapperService
{
    private readonly ILogger<ScrapperProcessService> _logger;

    public bool IsScrapping { get; private set; }
    public StreamEnvironment Environment { get; private set; }
    public int MaxFails { get; private set; }
    public CancellationTokenSource Cts { get; private set; }
    public int DelayInSeconds { get; set; }


    private readonly IBrowserService _browser;
    private readonly IFileService _file;
    private readonly IProcessService _processService;

    public string Website { get; private set; }
    public string Livestream { get; private set; }
    public string CurrentGame { get; private set; }

    public int ViewersCount { get; private set; }

    public event IScrapperService.StatusChangeEventHandler? StatusChangeEvent;


    //Constructor
    public ScrapperProcessService(ILogger<ScrapperProcessService> logger, IBrowserService browser, IFileService file, IProcessService process)
    {
        _logger = logger;
        Environment = new();
        Website = "";
        Livestream = "";
        CurrentGame = "";
        ViewersCount = 0;
        _browser = browser;
        _file = file;
        _processService = process;
        Cts = new();
        IsScrapping = false;
        MaxFails = 3;
        DelayInSeconds = 60;
    }

    private void StartScrapperBrowser()
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
    }

    private bool OpenScrapper()
    {
        //OpenBrowser
        try
        {
            StartScrapperBrowser();

            if (_browser.IsReady && _browser.Browser != null)
            {
                if (PrepareScrapperPage())
                {
                    _logger.LogInformation("Browser page is ready for {website}/{livestream}", Website, Livestream);
                    IsScrapping = true;
                }
                else
                {
                    _logger.LogWarning("Browser page is not ready for {website}/{livestream}", Website, Livestream);
                    Stop();
                    IsScrapping = false;
                }
            }
            else
            {
                _logger.LogWarning("Browser page is not ready for {website}/{livestream}", Website, Livestream);
                Stop();
                IsScrapping = false;
            }
            return IsScrapping;
        }
        catch (Exception)
        {
            _logger.LogWarning("Browser page is not ready for {website}/{livestream}", Website, Livestream);
            Stop();
            IsScrapping = false;
            return IsScrapping;
        }

    }

    private void CreateInfoLog()
    {
        StringBuilder sb = new();
        sb.Append($"Stream: {Website}/{Livestream} | ");
        sb.Append($"Playing: {CurrentGame} | ");
        sb.Append($"Viewers Count: {ViewersCount}");

        _logger.LogInformation("{message}", sb.ToString());
    }

    public Task RunTestAsync(StreamEnvironment environment, string livestream, int minutes)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> RunScrapperAsync(StreamEnvironment environment, string livestream, int index = -1)
    {
        if (!IsScrapping)
        {
            Environment = environment;
            Website = Environment.Website;
            Livestream = livestream;

            bool hasStarted = await Task.Run(() => Start());

            if (hasStarted)
            {
                await Task.Run(() =>
                {
                    Func<Task> func = new(() => RunAsync(index));
                    FuncProcess funcProcess = new(index, OperationProcess.RunScrapper, func);
                    _processService.RunQueue.Enqueue(funcProcess);
                });

                IsScrapping = true;

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
        if (Cts.IsCancellationRequested)
        {
            Cts = new CancellationTokenSource();
        }

        bool isOpen = OpenScrapper();

        if (isOpen)
        {
            _logger.LogInformation("Scrapper has started for {website}/{livestream}", Website, Livestream);
        }
        else
        {
            _logger.LogWarning("Scrapper failed to start for {website}/{livestream}", Website, Livestream);
        }
        return isOpen;
    }

    public void Stop()
    {
        Cts.Cancel();
        IsScrapping = false;
        StatusChangeEvent?.Invoke();

        if (_browser != null)
        {
            _browser.StopBrowserPage();
        }

        if (IsScrapping)
        {
            _logger.LogInformation("Scrapper has stopped for {website}/{livestream}", Website, Livestream);
        }
    }

    private void Hold()
    {
        if (_browser != null)
        {
            _browser.StopBrowserPage();
        }
    }

    private int? ReadViewerCounter()
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
            _logger.LogError(e, "ReadViewerCounter");
            return null;
        }
    }

    private string? ReadCurrentGame()
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
        catch (Exception)
        {
            _logger.LogWarning("Element for Current Game was not found. ({locator})", Environment.GameContainer);
            return null;
        }
    }

    private async Task RunAsync(int index)
    {
        try
        {
            string? currentGame = null;
            int? counter = null;
            bool result;
            DateTime start = DateTime.Now;

            //Failed atempts
            if (_browser.Browser == null)
            {
                result = await Task.Run(() => OpenScrapper(), Cts.Token);
            }
            else
            {
                result = true;
            }

            //Main loop
            if (result)
            {
                for (int failedAtempts = 0; failedAtempts < MaxFails; failedAtempts++)
                {
                    //Local variables
                    counter = await Task.Run(() => ReadViewerCounter(), Cts.Token);
                    currentGame = await Task.Run(() => ReadCurrentGame(), Cts.Token);

                    if (counter.HasValue && counter <= 0 || string.IsNullOrEmpty(currentGame))
                    {
                        failedAtempts++;
                        await Task.Delay(5000, Cts.Token);
                    }
                    else
                    {
                        break;
                    }
                }

                if (counter.HasValue && counter <= 0 || string.IsNullOrEmpty(currentGame))
                {
                    _logger.LogWarning("Scrapper failed and has to stop for {website}/{livestream}", Website, Livestream);
                    _ = Task.Run(() => Stop(), CancellationToken.None);
                }
                else
                {
                    //Flush data
                    Task task = Task.Run(() =>
                    {
                        string result = string.Concat(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), ",", currentGame, ",", counter);
                        List<string> newList = new()
                        {
                        result
                        };
                        WriteData(newList, Environment.Website, Livestream, "counters");
                    }, Cts.Token);
                    CreateInfoLog();
                    await task;

                    await Task.Run(() => Hold(), CancellationToken.None);

                    TimeSpan timeSpan = DateTime.Now - start;
                    if (timeSpan.TotalMilliseconds < (DelayInSeconds * 1000))
                    {
                        await Task.Delay((int)((DelayInSeconds * 1000) - timeSpan.TotalMilliseconds), Cts.Token);
                    }

                    if (!Cts.Token.IsCancellationRequested)
                    {
                        Func<Task> func = new(() => RunAsync(index));
                        FuncProcess funcProcess = new(index, OperationProcess.RunScrapper, func);
                        _processService.RunQueue.Enqueue(funcProcess);
                    }
                }
            }
            else
            {
                _ = Task.Run(() => Stop(), CancellationToken.None);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("RunAsync in ScrapperProcessService was cancelled");
            _ = Task.Run(() => Stop(), CancellationToken.None);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "RunAsync in ScrapperProcessService finished with error");
            _ = Task.Run(() => Stop(), CancellationToken.None);
        }
    }

    private void WriteData(List<string> lines, string website, string livestream, string type, bool startNew = false)
    {
        string file = $"{ServiceUtils.RemoveSpecial(website.ToLower())}-{ServiceUtils.RemoveSpecial(livestream.ToLower())}-{type}.csv";
        _file.WriteCsv("files/csv", file, lines, startNew);
    }

    //Handle page start by environment
    private bool PrepareScrapperPage()
    {
        bool result = false;
        switch (Environment.Website)
        {
            case "facebook":
                try
                {
                    if (_browser.Browser != null)
                    {
                        WebElement? webElementOpen = _browser.WaitUntilElementClickable(Environment.OpenLive);
                        if (webElementOpen != null)
                        {
                            webElementOpen.Click();
                        }

                        if (_browser.WaitUntilElementVisible(Environment.ReadyCheck) != null && _browser.WaitUntilElementVisible(Environment.GameContainer) != null)
                        {
                            result = true;
                        }
                        else
                        {
                            result = false;
                        }
                    }
                }
                catch (Exception)
                {                    
                    _logger.LogWarning("Prepare scrapper page failed.");
                    result = false;
                }
                break;
            case "youtube":
                result = true;
                break;
            case "twitch":
                result = true;
                break;
            default:
                result = false;
                break;
        }
        return result;
    }

    public void Dispose()
    {
        Stop();
        _browser.Dispose();
    }
}
