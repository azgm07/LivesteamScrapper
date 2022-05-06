using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace LivesteamScrapper.Controllers
{
    public class BrowserController : Controller
    {
        private readonly ILogger<Controller> _logger;
        public bool IsScrapping { get; set; }
        public bool IsBrowserOpened { get; set; }
        public  ChromeDriver Browser { get; private set; }
        public string OpenedUrl { get; set; }

        private bool _isReady;
        public bool IsReady
        {
            get
            {
                return _isReady;
            }
            private set
            {
                _isReady = value; if (PropertyChanged != null)
                {
                    PropertyChanged?.Invoke(_isReady, EventArgs.Empty);
                }
            }
        }

        //Events
        public event EventHandler? PropertyChanged;

        //Constructor
        public BrowserController(ILogger<Controller> logger, bool isHeadless = true)
        {
            _logger = logger;
            OpenedUrl = string.Empty;

            //Returns a new BrowserPage
            ChromeOptions options = new ChromeOptions()
            {
                //BinaryLocation = "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe"
            };

            //Change options depending on the case
            if (isHeadless)
            {
                options.AddArguments(new List<string>() { "headless", "disable-gpu", "no-sandbox", "disable-extensions", "log-level=3" });
            }
            else
            {
                options.AddArguments(new List<string>() { /*"headless",*/ "disable-gpu", "no-sandbox", "disable-extensions", "log-level=3" });
            }

            Browser = new ChromeDriver(options);
        }
        //Finalizer
        ~BrowserController()
        {
            if (Browser != null)
            {
                Browser.Dispose();
            }
        }

        public IWebElement WaitUntilElementExists(By elementLocator, int timeout = 10)
        {
            try
            {
                var wait = new WebDriverWait(Browser, TimeSpan.FromSeconds(timeout));
                return wait.Until(ExpectedConditions.ElementExists(elementLocator));
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("Element with locator: '" + elementLocator + "' was not found in current context page.");
                throw;
            }
        }

        public IWebElement WaitUntilElementVisible(By elementLocator, int timeout = 10)
        {
            try
            {
                var wait = new WebDriverWait(Browser, TimeSpan.FromSeconds(timeout));
                return wait.Until(ExpectedConditions.ElementIsVisible(elementLocator));
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("Element with locator: '" + elementLocator + "' was not found.");
                throw;
            }
        }

        public IWebElement WaitUntilElementClickable(By elementLocator, int timeout = 10)
        {
            try
            {
                var wait = new WebDriverWait(Browser, TimeSpan.FromSeconds(timeout));
                return wait.Until(ExpectedConditions.ElementToBeClickable(elementLocator));
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("Element with locator: '" + elementLocator + "' was not found.");
                throw;
            }
        }

        public void OpenBrowserPage(string url, By? waitSelector = null)
        {
            try
            {
                if (Browser.Url != url)
                {
                    IsReady = false;                    
                    Browser.Navigate().GoToUrl(url);
                    if (waitSelector != null)
                    {
                        WaitUntilElementExists(waitSelector, 30);
                    }
                    OpenedUrl = url;
                    IsReady = true;
                }
            }
            catch (Exception)
            {
                IsReady = false;
                throw;
            }
        }
        
        public void ReloadBrowserPage(By? waitSelector = null)
        {
            try
            {
                if (Browser != null)
                {
                    IsReady = false;

                    Browser.Navigate().Refresh();
                    if (waitSelector != null)
                    {
                        WaitUntilElementExists(waitSelector);
                    }
                    IsReady = true;
                }
            }
            catch (Exception)
            {
                IsReady = false;
                throw;
            }
        }
    }
}