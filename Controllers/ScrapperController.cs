using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using PuppeteerSharp;

namespace LivesteamScrapper.Controllers
{
    public class ScrapperController : Controller
    {
        private readonly ILogger<Controller> _logger;
        public bool IsScrapping { get; set; }
        public bool IsBrowserOpened { get; set; }
        private string _chromePath;
        private Browser? browser;
        private Page? page;
        public ScrapperController(ILogger<Controller> logger, string chromePath)
        {
            _logger = logger;
            _chromePath = chromePath;
        }

        public async Task OpenBrowserPage(string fullUrl, string waitSelector)
        {
            try
            {
                //Load a new Browser if browser is not open
                if (browser == null)
                {
                    //Load new virtual browser with local chrome
                    var options = new LaunchOptions()
                    {
                        Headless = true,
                        ExecutablePath = _chromePath
                    };

                    browser = await Puppeteer.LaunchAsync(options, null);
                }

                //Check if the page is open on the same url, if not open it
                if (page != null && page.Url == fullUrl)
                {
                    return;
                }
                else
                {
                    //Close the current page before open a new one if not null.
                    if (page != null)
                    {
                        await page.CloseAsync();
                    }
                    page = await browser.NewPageAsync();

                    //Go to the page url and wait for DOMContentLoaded event
                    List<WaitUntilNavigation> navArray = new List<WaitUntilNavigation>();
                    navArray.Add(WaitUntilNavigation.DOMContentLoaded);
                    var waitNavigation = new NavigationOptions()
                    {
                        WaitUntil = navArray.ToArray(),
                        Timeout = 60000
                    };
                    await page.GoToAsync(fullUrl, waitNavigation);

                    //Wait for specific selector to load
                    var waitOptions = new WaitForSelectorOptions()
                    {
                        Visible = true,
                        Timeout = 60000
                    };
                    await page.WaitForSelectorAsync(waitSelector, waitOptions);
                    Console.WriteLine("Page is ready");
                }

                IsBrowserOpened = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        public async Task CloseBrowserPage()
        {
            if(page != null)
            {
                await page.CloseAsync();
                page = null;
            }
            if(browser != null)
            {
                await browser.CloseAsync();
                browser = null;
            }

            IsBrowserOpened = false;
        }

        public async Task<List<ChatMessageModel>> ReadChat()
        {
            //Verify if is already scrapping and return case not
            if (IsScrapping)
            {
                return new List<ChatMessageModel>();
            }
            //Verify if the browser is already open with a page
            if (page == null)
            {
                Console.WriteLine("Page not open");
                return new List<ChatMessageModel>();
            }
            try
            {
                IsScrapping = true;
                //Retrive new comments
                List<ChatMessageModel> scrapeMessages = new List<ChatMessageModel>();
                var messages = await page.QuerySelectorAllAsync(".message");
                foreach (var message in messages)
                {
                    ChatMessageModel newMessage = new ChatMessageModel();
                    string messageAuthor = await message.EvaluateFunctionAsync<string>("()=>document.querySelector('span[class=\"username color-grey\"]').innerHTML");
                    string messageContent = await message.EvaluateFunctionAsync<string>("()=>document.querySelector('span[class=\"message-text\"]').innerHTML");

                    newMessage.Author = messageAuthor;
                    newMessage.Content = messageContent;

                    scrapeMessages.Add(newMessage);
                }
                Console.WriteLine($"List of messages: {scrapeMessages.ToString()}");
                return scrapeMessages;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            finally
            {
                IsScrapping = false;
            }
        }

        public async Task<int?> ReadViewerCounter()
        {
            //Verify if the browser is already open with a page
            if (page == null)
            {
                Console.WriteLine("Page not open");
                return null;
            }
            try
            {
                //Retrive new comments
                int? viewersCount = null;
                var counterText = await page.EvaluateFunctionAsync<string>("()=>document.querySelector('span[class=\"viewer-count\"]').innerHTML");
                
                if(int.TryParse(counterText, out int result))
                {
                    viewersCount = result;
                }

                Console.WriteLine($"Count: {viewersCount.ToString()}");
                return viewersCount;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }        
    }
}