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
        public  ChromeDriver? Browser { get; private set; }
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
        public BrowserController(ILogger<Controller> logger)
        {
            _logger = logger;
            OpenedUrl = string.Empty;
            Browser = null;
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
                if (Browser == null || Browser.Url != url)
                {
                    IsReady = false;
                    //Returns a new BrowserPage
                    ChromeOptions options = new ChromeOptions()
                    {
                        //BinaryLocation = "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe"
                    };
                    options.AddArguments(new List<string>() { /*"headless",*/ "disable-gpu", "no-sandbox", "maximize-window", "log-level=3" });
                    Browser = new ChromeDriver(options);
                    Browser.Navigate().GoToUrl(url);
                    if (waitSelector != null)
                    {
                        WaitUntilElementVisible(waitSelector);
                    }
                    OpenedUrl = url;
                    IsReady = true;
                }
            }
            catch (Exception)
            {
                IsReady = false;
                if (Browser != null)
                {
                    Browser.Dispose();
                    Browser = null;
                }

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
                        WaitUntilElementVisible(waitSelector);
                    }
                    IsReady = true;
                }
            }
            catch (Exception)
            {
                IsReady = false;
                if (Browser != null)
                {
                    Browser.Dispose();
                    Browser = null;
                }

                throw;
            }
        }
    }
}