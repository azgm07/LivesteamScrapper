using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrapperLibrary.Controllers
{
    internal class DriverController
    {
        private readonly ILogger<DriverController> _logger;

        public DriverController(ILogger<DriverController> logger)
        {
            _logger = logger;
        }

        public WebElement? WaitUntilElementExists(WebDriver driver, By elementLocator, int timeout = 10)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));
                return (WebElement)wait.Until(ExpectedConditions.ElementExists(elementLocator));
            }
            catch (NoSuchElementException)
            {
                _logger.LogWarning("Element locator ({locator}) was not found in current context page.", elementLocator);
                return null;
            }
        }

        public WebElement? WaitUntilElementVisible(WebDriver driver, By elementLocator, int timeout = 10)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));
                return (WebElement)wait.Until(ExpectedConditions.ElementIsVisible(elementLocator));
            }
            catch (NoSuchElementException)
            {
                _logger.LogWarning("Element locator ({locator}) was not visible.", elementLocator);
                return null;
            }
        }

        public WebElement? WaitUntilElementClickable(WebDriver driver, By elementLocator, int timeout = 10)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));
                return (WebElement)wait.Until(ExpectedConditions.ElementToBeClickable(elementLocator));
            }
            catch (NoSuchElementException)
            {
                _logger.LogWarning("Element locator ({locator}) was not clickable.", elementLocator);
                return null;
            }
        }

        public WebDriver? CreateDriver(bool isHeadless = true)
        {
            try
            {
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

                return new ChromeDriver(driverService, options);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Could not create driver");
                return null;
            }
        }
    }
}
