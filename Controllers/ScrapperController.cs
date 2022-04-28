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
        private ChromeDriver? browser;

        //Constructor
        public ScrapperController(ILogger<Controller> logger)
        {
            _logger = logger;
        }
        //Finalizer
        ~ScrapperController()
        {
            if (browser != null)
            {
                browser.Dispose();
            }
        }

        public void OpenBrowserPage(string fullUrl, string waitSelector)
        {
            try
            {
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
                    wait.Until(ExpectedConditions.ElementExists(By.ClassName(waitSelector)));
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
                BlockingCollection<ChatMessageModel> scrapeMessages = new BlockingCollection<ChatMessageModel>();
                var chat = browser.FindElement(By.XPath("//*[@id=\"root\"]/div/div/div[2]/div[4]/div[1]/div/div/div[4]/div[1]/div[1]/div[1]"));
                var messages = chat.FindElements(By.ClassName("message"));
                Console.WriteLine($"Messages found: {messages.Count}");
                Parallel.ForEach(messages, (message) =>
                {
                    try
                    {
                        ChatMessageModel newMessage = new ChatMessageModel();
                        string messageAuthor = message.FindElement(By.CssSelector("div > div > span.components-chatbox-user-menu > span")).Text;
                        string messageContent = message.FindElement(By.CssSelector("div > div > span.message-text")).Text;

                        newMessage.Author = messageAuthor;
                        newMessage.Content = messageContent;

                        scrapeMessages.TryAdd(newMessage);

                    }
                    catch { } //Ignore errors of FindElement
                });

                return scrapeMessages.ToList();
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
                var counter = browser.FindElement(By.CssSelector("#layout-content > div > div > div.channel-top-bar > div > div.components-profile-card-center.only-center > div.channel-infos > span > span"));
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