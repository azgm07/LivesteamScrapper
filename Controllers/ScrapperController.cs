using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace LivesteamScrapper.Controllers
{
    public class ScrapperController : Controller
    {
        private readonly ILogger<Controller> _logger;
        public bool IsScrapping { get; set; }
        public bool IsBrowserOpened { get; set; }
        public readonly EnvironmentModel environment;
        private ChromeDriver? browser;
        private string lastMessage = "";

        public string Website { get; set; }
        public string Livestream { get; set; }

        //Constructor
        public ScrapperController(ILogger<Controller> logger, EnvironmentModel environment, string website, string livestream)
        {
            _logger = logger;
            this.environment = environment;
            Website = website;
            Livestream = livestream;
        }
        //Finalizer
        ~ScrapperController()
        {
            if (browser != null)
            {
                browser.Dispose();
            }
        }

        public void OpenBrowserPage()
        {
            try
            {
                lastMessage = "";
                string fullUrl = environment.Http + Livestream;
                if (browser == null || browser.Url != fullUrl)
                {
                    //Returns a new BrowserPage
                    ChromeOptions options = new ChromeOptions()
                    {
                        //BinaryLocation = "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe"
                    };
                    options.AddArguments(new List<string>() { "headless", "disable-gpu", "log-level=3" });
                    browser = new ChromeDriver(options);

                    WebDriverWait wait = new WebDriverWait(browser, TimeSpan.FromSeconds(10));
                    browser.Navigate().GoToUrl(fullUrl);
                    wait.Until(ExpectedConditions.ElementExists(By.ClassName(environment.Selector)));
                    ConsoleController.ShowBrowserLog(EnumsModel.BrowserLog.Ready);
                }
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
                throw;
            }
        }

        public (List<ChatMessageModel>, int lastIndex) ReadChat()
        {
            //Verify if is already scrapping and return case not
            if (IsScrapping)
            {
                return (new List<ChatMessageModel>(), 0);
            }
            //Verify if the browser is already open with a page
            if (browser == null)
            {
                ConsoleController.ShowBrowserLog(EnumsModel.BrowserLog.NotReady);
                return (new List<ChatMessageModel>(), 0);
            }

            try
            {
                IsScrapping = true;
                //Retrive new comments
                List<ChatMessageModel> scrapeMessages = new List<ChatMessageModel>();
                var chat = browser.FindElement(environment.ChatContainer);
                var messages = chat.FindElements(environment.MessageContainer);

                //Transform all messages to a list in order
                foreach (var message in messages)
                {
                    ChatMessageModel newMessage = new ChatMessageModel();
                    string messageAuthor, messageContent;

                    try
                    {
                        messageAuthor = message.FindElement(environment.MessageAuthor).Text;
                    }
                    catch 
                    {
                        messageAuthor = "";
                    }

                    try
                    {
                        messageContent = message.FindElement(environment.MessageContent).Text;
                    }
                    catch
                    {
                        messageContent = "";
                    }

                    if(!string.IsNullOrEmpty(messageAuthor))
                    {
                        newMessage.Author = messageAuthor;
                        newMessage.Content = messageContent;

                        scrapeMessages.Add(newMessage);
                    }
                }
                
                ConsoleController.Chat.MessagesFound = scrapeMessages.Count;

                //Limits the return list based on the lastmessage found
                int lastIndex = -1;
                List<ChatMessageModel> returnMessages;

                if(!string.IsNullOrEmpty(lastMessage) && scrapeMessages.Count > 0)
                {
                    lastIndex = scrapeMessages.FindLastIndex(item => string.Concat(item.Author, " - ", item.Content) == lastMessage);
                    lastMessage = string.Concat(scrapeMessages.Last().Author, " - ", scrapeMessages.Last().Content);
                }
                else if(scrapeMessages.Count > 0)
                {
                    lastMessage = string.Concat(scrapeMessages.Last().Author, " - ", scrapeMessages.Last().Content);
                }

                if (scrapeMessages.Count > 0 && scrapeMessages.Count - 1 != lastIndex)
                {
                    returnMessages = scrapeMessages.GetRange(lastIndex + 1, scrapeMessages.Count - (lastIndex + 1));
                }
                else
                {
                    returnMessages = new List<ChatMessageModel>();
                }

                if(!string.IsNullOrEmpty(lastMessage))
                {
                    ConsoleController.Chat.LastMessage = lastMessage;
                }
                return (returnMessages, lastIndex);
            }
            catch (Exception e)
            {
                ConsoleController.ShowExceptionLog(e.Message);
                return (new List<ChatMessageModel>(), 0);
            }
            finally
            {
                IsScrapping = false;
            }
        }

        public int? ReadViewerCounter()
        {
            //Verify if the browser is already open with a page
            if (browser == null)
            {
                ConsoleController.ShowBrowserLog(EnumsModel.BrowserLog.NotReady);
                return null;
            }
            try
            {
                //Retrive new comments
                int viewersCount = 0;
                var counter = browser.FindElement(environment.CounterContainer);
                string counterText = counter.GetAttribute("textContent");
                counterText = Regex.Replace(counterText, "[^0-9]", "");
                if (int.TryParse(counterText, out int result))
                {
                    viewersCount = result;
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
            if (browser == null)
            {
                ConsoleController.ShowBrowserLog(EnumsModel.BrowserLog.NotReady);
                return null;
            }
            try
            {
                //Retrive new comments
                var game = browser.FindElement(environment.GameContainer);
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
    }
}