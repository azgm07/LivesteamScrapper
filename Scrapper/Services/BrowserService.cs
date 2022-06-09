using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Scrapper.Services;

public interface IBrowserService
{
    public bool IsScrapping { get; }
    public bool IsBrowserOpened { get; }
    public bool IsReady { get; }
    public WebDriver? Browser { get; }
    public string OpenedUrl { get; }

    public IWebElement WaitUntilElementExists(By elementLocator, int timeout = 10);
    public IWebElement WaitUntilElementVisible(By elementLocator, int timeout = 10);
    public IWebElement WaitUntilElementClickable(By elementLocator, int timeout = 10);
    public void StartBrowser(bool isHeadless = true);
    public void OpenBrowserPage(string url, By? waitSelector = null);
    public void ReloadBrowserPage(By? waitSelector = null);
    public void StopBrowserPage();

}

public sealed class BrowserService : IBrowserService
{
    private readonly ILogger<BrowserService> _logger;
    public bool IsScrapping { get; private set; }
    public bool IsBrowserOpened { get; private set; }
    public WebDriver? Browser { get; private set; }
    public string OpenedUrl { get; private set; }

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
    public BrowserService(ILogger<BrowserService> logger)
    {
        _logger = logger;
        OpenedUrl = string.Empty;
    }

    public void StartBrowser(bool isHeadless = true)
    {
        //Returns a new BrowserPage
        ChromeOptions options = new()
        {
            //BinaryLocation = "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe"
        };

        //Change options depending on the case
        if (isHeadless)
        {
            options.AddArguments(new List<string>() { "headless", "disable-gpu", "no-sandbox", "disable-extensions",
                "disable-application-cache", "disable-notifications", "disable-infobars", "log-level=3", "mute-audio" });
        }
        else
        {
            options.AddArguments(new List<string>() { /*"headless",*/ "disable-gpu", "no-sandbox", "disable-extensions",
                "disable-application-cache", "disable-notifications", "disable-infobars", "log-level=3", "mute-audio" });
        }
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { images = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { cookies = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { plugins = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { popups = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { geolocation = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { notifications = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { mixed_script = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { media_stream = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { media_stream_mic = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { media_stream_camera = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { protocol_handlers = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { ppapi_broker = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { automatic_downloads = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { midi_sysex = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { push_messaging = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { ssl_cert_decisions = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { metro_switch_to_desktop = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { protected_media_identifier = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { app_banner = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { site_engagement = 2 } });
        options.AddUserProfilePreference("profile", new { default_content_setting_values = new { durable_storage = 2 } });
        Browser = new ChromeDriver(options);
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
            _logger.LogError("Element locator ({locator}) was not found in current context page.", elementLocator);
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
            _logger.LogError("Element locator ({locator}) was not found.", elementLocator);
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
            _logger.LogError("Element locator ({locator}) was not found.", elementLocator);
            throw;
        }
    }

    public void OpenBrowserPage(string url, By? waitSelector = null)
    {
        try
        {
            if (Browser != null && Browser.Url != url)
            {
                IsReady = false;
                Browser.Navigate().GoToUrl(url);
                if (waitSelector != null)
                {
                    WaitUntilElementExists(waitSelector);
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

    public void StopBrowserPage()
    {
        if (Browser != null)
        {
            Browser.Dispose();
        }
    }
}
