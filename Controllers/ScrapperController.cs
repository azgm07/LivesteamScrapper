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

        //Constructor
        public ScrapperController(ILogger<Controller> logger, EnvironmentModel environment)
        {
            _logger = logger;
            this.environment = environment;
        }
        //Finalizer
        ~ScrapperController()
        {
            if (browser != null)
            {
                browser.Dispose();
            }
        }

        public void OpenBrowserPage(string path)
        {
            try
            {
                lastMessage = "";
                string fullUrl = environment.Http + path;
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
                    Console.WriteLine("Page is ready");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        public List<ChatMessageModel> ReadChat()
        {
            //Verify if is already scrapping and return case not
            if (IsScrapping)
            {
                return new List<ChatMessageModel>();
            }
            //Verify if the browser is already open with a page
            if (browser == null)
            {
                Console.WriteLine("Page not open");
                return new List<ChatMessageModel>();
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
                
                Console.WriteLine($"Messages found: {scrapeMessages.Count}");

                //Limits the return list based on the lastmessage found
                int index = -1;
                List<ChatMessageModel> returnMessages;

                if(!string.IsNullOrEmpty(lastMessage) && scrapeMessages.Count > 0)
                {
                    index = scrapeMessages.FindLastIndex(item => string.Concat(item.Author, " - ", item.Content) == lastMessage);                  
                    Console.WriteLine($"Last message index: {index + 1}");
                    Console.WriteLine($"Last message found: {lastMessage}");
                    lastMessage = string.Concat(scrapeMessages[scrapeMessages.Count - 1].Author, " - ", scrapeMessages[scrapeMessages.Count - 1].Content);
                }
                else if(scrapeMessages.Count > 0)
                {
                    Console.WriteLine($"Last message index: {index + 1}");
                    Console.WriteLine($"Last message found: {lastMessage}");
                    lastMessage = string.Concat(scrapeMessages[scrapeMessages.Count - 1].Author, " - ", scrapeMessages[scrapeMessages.Count - 1].Content);
                }
                else
                {
                    Console.WriteLine($"Last message index: {index + 1}");
                    Console.WriteLine($"Last message found: {lastMessage}");
                }

                if (scrapeMessages.Count > 0 && scrapeMessages.Count - 1 != index)
                {
                    returnMessages = scrapeMessages.GetRange(index + 1, scrapeMessages.Count - (index + 1));
                }
                else
                {
                    returnMessages = new List<ChatMessageModel>();
                }
                
                Console.WriteLine($"Return count: {returnMessages.Count}");
                return returnMessages;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new List<ChatMessageModel>();
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
                Console.WriteLine("Page not open");
                return null;
            }
            try
            {
                //Retrive new comments
                int? viewersCount = null;
                var counter = browser.FindElement(environment.CounterContainer);
                string counterText = counter.GetAttribute("textContent");
                counterText = Regex.Replace(counterText, "[^0-9]", "");
                if (int.TryParse(counterText, out int result))
                {
                    viewersCount = result;
                }

                Console.WriteLine($"Count: {viewersCount}");
                return viewersCount;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
}