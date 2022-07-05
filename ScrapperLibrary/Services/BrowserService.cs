using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Scrapper.Services;

public interface IBrowserService : IDisposable
{
    public bool IsScrapping { get; }
    public bool IsBrowserOpened { get; }
    public bool IsReady { get; }
    public WebDriver? Browser { get; }
    public string OpenedUrl { get; }

    public WebElement? WaitUntilElementExists(By elementLocator, int timeout = 10);
    public WebElement? WaitUntilElementVisible(By elementLocator, int timeout = 10);
    public WebElement? WaitUntilElementClickable(By elementLocator, int timeout = 10);
    public void StartBrowser(bool isHeadless = true);
    public bool OpenBrowserPage(string url, By? waitSelector = null);
    public bool ReloadBrowserPage(By? waitSelector = null);
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
        try
        {
            if (Browser != null)
            {
                Browser.Quit();
            }

            //Returns a new BrowserPage
            ChromeOptions options = new()
            {
                //BinaryLocation = "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe"
            };

            var driverService = ChromeDriverService.CreateDefaultService();

            //Change options depending on the case
            if (isHeadless)
            {
                options.AddArguments(new List<string>() { "headless", "disable-gpu", "no-sandbox", "silent-launch", "no-startup-window", "disable-extensions",
                "disable-application-cache", "disable-notifications", "disable-infobars", "log-level=3", "mute-audio" });

                driverService.HideCommandPromptWindow = true;
            }
            else
            {
                options.AddArguments(new List<string>() { /*"headless",*/ "disable-gpu", "no-sandbox", "disable-extensions",
                "disable-application-cache", "disable-notifications", "disable-infobars", "log-level=3", "mute-audio" });
            }

            options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.cookies", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.plugins", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.popups", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.geolocation", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.media_stream", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.media_stream_mic", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.media_stream_camera", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.protocol_handlers", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.ppapi_broker", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.midi_sysex", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.push_messaging", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.ssl_cert_decisions", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.metro_switch_to_desktop", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.protected_media_identifier", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.app_banner", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.site_engagement", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.durable_storage", 2);

            Browser = new ChromeDriver(driverService, options);

            _logger.LogInformation("Browser opened for {url}", OpenedUrl);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not open browser for {url}", OpenedUrl);
        }
    }

    public WebElement? WaitUntilElementExists(By elementLocator, int timeout = 10)
    {
        try
        {
            var wait = new WebDriverWait(Browser, TimeSpan.FromSeconds(timeout));
            return (WebElement)wait.Until(ExpectedConditions.ElementExists(elementLocator));
        }
        catch (NoSuchElementException)
        {
            _logger.LogWarning("Element locator ({locator}) was not found in current context page.", elementLocator);
            return null;
        }
    }

    public WebElement? WaitUntilElementVisible(By elementLocator, int timeout = 10)
    {
        try
        {
            var wait = new WebDriverWait(Browser, TimeSpan.FromSeconds(timeout));
            return (WebElement)wait.Until(ExpectedConditions.ElementIsVisible(elementLocator));
        }
        catch (NoSuchElementException)
        {
            _logger.LogWarning("Element locator ({locator}) was not visible.", elementLocator);
            return null;
        }
    }

    public WebElement? WaitUntilElementClickable(By elementLocator, int timeout = 10)
    {
        try
        {
            var wait = new WebDriverWait(Browser, TimeSpan.FromSeconds(timeout));
            return (WebElement)wait.Until(ExpectedConditions.ElementToBeClickable(elementLocator));
        }
        catch (NoSuchElementException)
        {
            _logger.LogWarning("Element locator ({locator}) was not clickable.", elementLocator);
            return null;
        }
    }

    public bool OpenBrowserPage(string url, By? waitSelector = null)
    {
        try
        {
            if (Browser != null && Browser.Url != url)
            {
                IsReady = false;
                Browser.Navigate().GoToUrl(url);
                if (waitSelector != null && WaitUntilElementExists(waitSelector) == null)
                {
                    IsReady = false;
                }
                else
                {
                    OpenedUrl = url;
                    IsReady = true;
                }
            }
        }
        catch (Exception)
        {
            IsReady = false;
            throw;
        }

        return IsReady;
    }

    public bool ReloadBrowserPage(By? waitSelector = null)
    {
        try
        {
            if (Browser != null)
            {
                IsReady = false;

                Browser.Navigate().Refresh();
                if (waitSelector != null && WaitUntilElementExists(waitSelector) == null)
                {
                    IsReady = false;
                }
                else
                {
                    IsReady = true;
                }
            }
        }
        catch (Exception)
        {
            IsReady = false;
            throw;
        }

        return IsReady;
    }

    public void StopBrowserPage()
    {
        try
        {
            if (Browser != null)
            {
                Browser.Quit();
                _logger.LogInformation("Browser closed for {url}", OpenedUrl);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not quit browser for {url}", OpenedUrl);
        }
        Browser = null;
    }

    public void Dispose()
    {
        if (Browser != null)
        {
            Browser.Quit();
        }
    }
}
