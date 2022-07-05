using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using Scrapper.Models;
using Scrapper.Utils;
using ScrapperLibrary.Interfaces;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using static Scrapper.Models.EnumsModel;

namespace Scrapper.Services;

public sealed class ScrapperInfoService : IScrapperService
{
    private readonly ILogger<ScrapperInfoService> _logger;

    public bool IsScrapping { get; private set; }
    public EnvironmentModel Environment { get; private set; }
    public int MaxFails { get; private set; }
    public CancellationTokenSource Cts { get; private set; }
    public int DelayInSeconds { get; set; }


    private readonly IBrowserService _browser;
    private readonly ITimeService _time;
    private readonly IFileService _file;

    private System.Timers.Timer timerTask;
    private bool isReloading;

    public string Website { get; private set; }
    public string Livestream { get; private set; }
    public string CurrentGame { get; private set; }

    public int ViewersCount { get; private set; }

    public event IScrapperService.StatusChangeEventHandler? StatusChangeEvent;

    //Constructor
    public ScrapperInfoService(ILogger<ScrapperInfoService> logger, IBrowserService browser, ITimeService time, IFileService file)
    {
        _logger = logger;
        Environment = new();
        Website = "";
        Livestream = "";
        CurrentGame = "";
        ViewersCount = 0;
        _browser = browser;
        _time = time;
        _file = file;
        timerTask = new();
        Cts = new();
        isReloading = false;
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
                if(PrepareScrapperPage())
                {
                    _logger.LogInformation("Browser page is ready for {website}/{livestream}", Website, Livestream);
                    IsScrapping = true;
                }
                else
                {
                    _logger.LogWarning("Browser page is not ready for {website}/{livestream}", Website, Livestream);
                    IsScrapping = false;
                }
            }
            else
            {
                _logger.LogWarning("Browser page is not ready for {website}/{livestream}", Website, Livestream);
                IsScrapping = false;
            }
            return IsScrapping;
        }
        catch (Exception)
        {
            _logger.LogWarning("Browser page is not ready for {website}/{livestream}", Website, Livestream);
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
                StartScrapperBrowser();
            }
            else
            {
                _browser.ReloadBrowserPage(Environment.Selector);
            }

            if (_browser.IsReady && _browser.Browser != null && !PrepareScrapperPage())
            {
                _logger.LogWarning("Browser page is not ready for {website}/{livestream}", Website, Livestream);
            }

            isReloading = false;
        }
        catch (Exception)
        {
            _logger.LogWarning("Browser page is not ready for {website}/{livestream}", Website, Livestream);
            isReloading = false;
        }

    }

    private async Task CreateInfoLogAsync(CancellationToken token, int delaySeconds = 30)
    {
        _logger.LogInformation("Console Started for {website}/{livestream}", Website, Livestream);
        await Task.Delay(delaySeconds * 1000, token);

        while (!token.IsCancellationRequested)
        {
            StringBuilder sb = new();
            sb.Append($"Stream: {Website}/{Livestream} | ");
            sb.Append($"Playing: {CurrentGame} | ");
            sb.Append($"Viewers Count: {ViewersCount}");

            _logger.LogInformation("{message}", sb.ToString());

            await Task.Delay(delaySeconds * 1000).ConfigureAwait(false);
        }

        _logger.LogInformation("Console Stopped for {website}/{livestream}", Website, Livestream);
    }

    public async Task RunTestAsync(EnvironmentModel environment, string livestream, int minutes)
    {
        Environment = environment;
        Website = Environment.Website;
        Livestream = livestream;
        bool hasStarted = await Task.Run(() => Start());
        if (hasStarted)
        {
            StartTimerTasksCancellation(minutes);

            List<Task> tasks = new()
            {
                RunDelayedAsync(Cts.Token),

                //Console Tasks
                CreateInfoLogAsync(Cts.Token, 30)
            };

            await Task.WhenAll(tasks);
        }

    }
    public async Task<bool> RunScrapperAsync(EnvironmentModel environment, string livestream, int index = -1)
    {
        if (!IsScrapping)
        {
            Environment = environment;
            Website = Environment.Website;
            Livestream = livestream;

            bool hasStarted = await Task.Run(() => Start());

            if (hasStarted)
            {
                List<Task> tasks = new()
                {
                    RunDelayedAsync(Cts.Token),
                    //Console Tasks
                    CreateInfoLogAsync(Cts.Token, 30)
                };

                _ = tasks;

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
            _logger.LogWarning("Scrapper failed to start for {website}/{livestream}", Website, Livestream);
            Stop();
        }
        return isOpen;
    }

    private void Hold()
    {
        if (_browser != null)
        {
            _browser.StopBrowserPage();
        }
    }

    public void Stop()
    {
        Cts.Cancel();
        StatusChangeEvent?.Invoke();
        IsScrapping = false;

        if (_browser != null)
        {
            _browser.StopBrowserPage();
        }

        if (IsScrapping)
        {
            _logger.LogInformation("Scrapper has stopped for {website}/{livestream}", Website, Livestream);
        }
    }

    private void StartTimerTasksCancellation(int minutes)
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

    private async Task RunDelayedAsync(CancellationToken token)
    {
        //Loop scrapping per sec.
        _time.Start("RunViewerScrapper");

        //Flush tasks
        List<Task> tasksFlush = new();

        List<int> listCounter = new();
        string? currentGame = null;
        int? counter = null;

        //Failed atempts
        int failedAtempts = 0;

        //Main loop
        while (!token.IsCancellationRequested)
        {
            DateTime start = DateTime.Now;

            //Break if too many fails
            if (failedAtempts >= MaxFails)
            {
                _logger.LogWarning("Scrapper failed and has to stop for {website}/{livestream}", Website, Livestream);
                break;
            }

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
                await Task.Delay(5000, CancellationToken.None);
                continue;
            }

            //Flush data
            Task task = Task.Run(() =>
            {
                string max = string.Concat(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), ",", currentGame, ",", listCounter.Max().ToString());
                List<string> newList = new()
                {
                    max
                };
                WriteData(newList, Environment.Website, Livestream, "counters");
                listCounter = new List<int>();
            }, CancellationToken.None);

            tasksFlush.Add(task);

            //Hold 1 min to continue than reload
            Task taskHold = Task.Run(() =>
            {
                Hold();
            }, CancellationToken.None);
            await taskHold;

            TimeSpan timeSpan = DateTime.Now - start;
            if (timeSpan.TotalMilliseconds < (DelayInSeconds * 1000))
            {
                await Task.Delay((int)((DelayInSeconds * 1000) - timeSpan.TotalMilliseconds), CancellationToken.None);
            }

            //Needs to reload the page
            Task taskRestart = Task.Run(() =>
            {
                ReloadScrapper();
            }, CancellationToken.None);
            await taskRestart;

            //Case reloading wait
            while (isReloading)
            {
                await Task.Delay(1000, CancellationToken.None);
            }
        }

        await Task.Run(() => Stop(), CancellationToken.None);

        //Stop timers
        await Task.Run(() => _time.Stop(), CancellationToken.None);

        //Send to file the rest of counter lines
        if (listCounter.Count > 0)
        {
            await Task.Run(() =>
            {
                string max = string.Concat(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), ",", currentGame, ",", listCounter.Max().ToString());
                List<string> newList = new()
                {
                    max
                };
                WriteData(newList, Environment.Website, Livestream, "counters");
            }, CancellationToken.None);
        }

        await Task.WhenAll(tasksFlush);
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
                    WebElement? webElementReady = _browser.WaitUntilElementExists(Environment.ReadyCheck);
                    if (_browser.Browser != null && webElementReady == null)
                    {
                        WebElement? webElementOpen = _browser.WaitUntilElementClickable(Environment.OpenLive);
                        if (webElementOpen != null)
                        {
                            webElementOpen.Click();
                            if (_browser.WaitUntilElementExists(Environment.ReadyCheck) != null)
                            {
                                result = true;
                            }
                            else
                            {
                                result = false;
                            }
                        }
                    }
                    else if(webElementReady != null)
                    {
                        result = true;
                    }
                    else
                    {
                        result = false;
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
