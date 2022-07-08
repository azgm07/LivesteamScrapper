using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using ScrapperLibrary.Interfaces;
using ScrapperLibrary.Models;
using ScrapperLibrary.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScrapperLibrary.Controllers
{
    public class TrackerController
    {
        public TrackerResponse LastResponse { get; private set; }

        private readonly ILogger<TrackerController> _logger;
        private readonly IFileService _fileService;
        private readonly DriverController _driver;
        private readonly StreamEnvironment _environment;
        private readonly string _url;
        private readonly int _maxFails;
        private readonly string _channel;

        public TrackerController(StreamEnvironment environment, string channel, ILogger<TrackerController> logger, ILoggerFactory loggerFactory, IFileService fileService, int maxFails = 3)
        {
            LastResponse = new();
            _environment = environment;
            _channel = channel;
            _url = $"{environment.Http}{channel}/live";

            _logger = logger;
            _fileService = fileService;
            _driver = new(loggerFactory.CreateLogger<DriverController>());
            _maxFails = maxFails;
        }

        public async Task<TrackerResponse?> GetInfoAsync(CancellationToken token)
        {
            TrackerResponse response = new();
            try
            {
                using (WebDriver? driver = _driver.CreateDriver())
                {
                    if (driver != null)
                    {
                        if (OpenPage(driver) && PreparePage(driver))
                        {
                            string? currentGame = null;
                            int? viewers = null;

                            for (int failedAtempts = 0; failedAtempts < _maxFails; failedAtempts++)
                            {
                                //Local variables
                                Task<int?> taskViewers = Task.Run(() => ReadViewers(driver), token);
                                Task<string?> taskGame = Task.Run(() => ReadCurrentGame(driver), token);

                                viewers = await taskViewers;
                                currentGame = await taskGame;

                                if (viewers.HasValue && viewers <= 0 || string.IsNullOrEmpty(currentGame))
                                {
                                    failedAtempts++;
                                    await Task.Delay(5000, token);
                                }
                                else
                                {
                                    break;
                                }
                            }

                            if (viewers == null || viewers.HasValue && viewers <= 0 || string.IsNullOrEmpty(currentGame))
                            {
                                return null;
                            }
                            else
                            {                                
                                response.CurrentGame = currentGame;
                                response.CurrentViewers = viewers.Value;

                                return response;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            LastResponse = response;
            return response;
        }

        private bool OpenPage(WebDriver driver)
        {
            driver.Navigate().GoToUrl(_url);
            if (_environment.Selector != null && _driver.WaitUntilElementExists(driver, _environment.Selector) == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool PreparePage(WebDriver driver)
        {
            bool result = false;
            switch (_environment.Website)
            {
                case "facebook":
                    try
                    {
                        if (driver != null)
                        {
                            WebElement? webElementOpen = _driver.WaitUntilElementClickable(driver, _environment.OpenLive);
                            if (webElementOpen != null)
                            {
                                webElementOpen.Click();
                            }

                            if (_driver.WaitUntilElementVisible(driver, _environment.ReadyCheck) != null &&
                                _driver.WaitUntilElementVisible(driver, _environment.GameContainer) != null)
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

        private int? ReadViewers(WebDriver driver)
        {
            try
            {
                //Retrive new comments
                int viewersCount = 0;
                var viewers = driver.FindElement(_environment.CounterContainer);
                string viewersText = viewers.GetAttribute("textContent");

                //Treat different types of text
                if (viewersText.IndexOf("mil") != -1 || viewersText.IndexOf("K") != -1)
                {
                    viewersText = Regex.Replace(viewersText, "[^0-9,.]", "");
                    viewersText = viewersText.Replace(".", ",");
                    if (decimal.TryParse(viewersText, out decimal result))
                    {
                        viewersCount = (int)(result * 1000);
                    }
                }
                else
                {
                    viewersText = Regex.Replace(viewersText, "[^0-9]", "");
                    if (int.TryParse(viewersText, out int result))
                    {
                        viewersCount = result;
                    }
                }

                return viewersCount;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "ReadViewers");
                return null;
            }
        }

        private string? ReadCurrentGame(WebDriver driver)
        {
            try
            {
                //Retrive new comments
                var game = driver.FindElement(_environment.GameContainer);
                string currentGame = game.GetAttribute("textContent");

                return currentGame;
            }
            catch (Exception)
            {
                _logger.LogWarning("Element for Current Game was not found. ({locator})", _environment.GameContainer);
                return null;
            }
        }
    }
}
