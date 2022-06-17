using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;

namespace Scrapper.Models;

public interface ILiveElementsInterface
{
    public abstract By CloseChatAnnouncement { get; set; }
    public abstract By ChannelName { get; set; }
    public abstract By OpenLive { get; set; }
}

public class LiveElementsModel : ILiveElementsInterface
{
    public By CloseChatAnnouncement { get; set; }
    public By ChannelName { get; set; }
    public By OpenLive { get; set; }

    protected readonly IConfiguration _config;

    public LiveElementsModel()
    {
        CloseChatAnnouncement = By.TagName(string.Empty);
        ChannelName = By.TagName(string.Empty);
        OpenLive = By.TagName(string.Empty);
        _config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
    }

    public static LiveElementsModel GetElements(string website = "")
    {
        return website.ToLower() switch
        {
            "booyah" => new BooyahElements(),
            "facebook" => new FacebookElements(),
            "twitch" => new TwitchElements(),
            "youtube" => new YoutubeElements(),
            _ => new LiveElementsModel(),
        };
    }
}
public class BooyahElements : LiveElementsModel
{
    public BooyahElements() : base()
    {
        CloseChatAnnouncement = By.CssSelector(_config.GetValue<string>($"LiveElements:{GetType().Name}:CloseChatAnnouncement"));
        ChannelName = By.CssSelector(_config.GetValue<string>($"LiveElements:{GetType().Name}:ChannelName"));
        OpenLive = By.CssSelector(_config.GetValue<string>($"LiveElements:{GetType().Name}:OpenLive"));
    }
}

public class FacebookElements : LiveElementsModel
{
    public FacebookElements() : base()
    {
        CloseChatAnnouncement = By.CssSelector(_config.GetValue<string>($"LiveElements:{GetType().Name}:CloseChatAnnouncement"));
        ChannelName = By.CssSelector(_config.GetValue<string>($"LiveElements:{GetType().Name}:ChannelName"));
        OpenLive = By.CssSelector(_config.GetValue<string>($"LiveElements:{GetType().Name}:OpenLive"));
    }
}

public class TwitchElements : LiveElementsModel
{
    public TwitchElements() : base()
    {
        CloseChatAnnouncement = By.CssSelector(_config.GetValue<string>($"LiveElements:{GetType().Name}:CloseChatAnnouncement"));
        ChannelName = By.CssSelector(_config.GetValue<string>($"LiveElements:{GetType().Name}:ChannelName"));
        OpenLive = By.CssSelector(_config.GetValue<string>($"LiveElements:{GetType().Name}:OpenLive"));
    }
}

public class YoutubeElements : LiveElementsModel
{
    public YoutubeElements() : base()
    {
        CloseChatAnnouncement = By.CssSelector(_config.GetValue<string>($"LiveElements:{GetType().Name}:CloseChatAnnouncement"));
        ChannelName = By.CssSelector(_config.GetValue<string>($"LiveElements:{GetType().Name}:ChannelName"));
        OpenLive = By.CssSelector(_config.GetValue<string>($"LiveElements:{GetType().Name}:OpenLive"));
    }
}
