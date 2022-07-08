using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;

namespace ScrapperLibrary.Models;

public interface IEnvironmentInterface
{
    public abstract string Http { get; set; }
    public abstract string Website { get; set; }
    public abstract By Selector { get; set; }
    public abstract By ChatContainer { get; set; }
    public abstract By MessageContainer { get; set; }
    public abstract By MessageAuthor { get; set; }
    public abstract By MessageContent { get; set; }
    public abstract By CounterContainer { get; set; }
    public abstract By GameContainer { get; set; }
    public abstract By CloseChatAnnouncement { get; set; }
    public abstract By ChannelName { get; set; }
    public abstract By OpenLive { get; set; }
    public abstract By ReadyCheck { get; set; }
}

public class StreamEnvironment : IEnvironmentInterface
{
    public string Http { get; set; }
    public string Website { get; set; }
    public By Selector { get; set; }
    public By ChatContainer { get; set; }
    public By MessageContainer { get; set; }
    public By MessageAuthor { get; set; }
    public By MessageContent { get; set; }
    public By CounterContainer { get; set; }
    public By GameContainer { get; set; }
    public By CloseChatAnnouncement { get; set; }
    public By ChannelName { get; set; }
    public By OpenLive { get; set; }
    public By ReadyCheck { get; set; }

    protected readonly IConfiguration _config;

    public StreamEnvironment()
    {
        Http = string.Empty;
        Website = string.Empty;
        Selector = By.TagName(string.Empty);
        ChatContainer = By.TagName(string.Empty);
        MessageContainer = By.TagName(string.Empty);
        MessageAuthor = By.TagName(string.Empty);
        MessageContent = By.TagName(string.Empty);
        CounterContainer = By.TagName(string.Empty);
        GameContainer = By.TagName(string.Empty);
        CloseChatAnnouncement = By.TagName(string.Empty);
        ChannelName = By.TagName(string.Empty);
        OpenLive = By.TagName(string.Empty);
        ReadyCheck = By.TagName(string.Empty);
        _config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
    }

    public enum Websites
    {
        Booyah,
        Facebook,
        Twitch,
        Youtube
    }

    public static StreamEnvironment GetEnvironment(string website = "")
    {
        return website.ToLower() switch
        {
            "booyah" => new Booyah(),
            "facebook" => new Facebook(),
            "twitch" => new Twitch(),
            "youtube" => new Youtube(),
            _ => new StreamEnvironment(),
        };
    }
}
public class Booyah : StreamEnvironment
{
    public Booyah() : base()
    {
        Http = _config.GetValue<string>($"Environment:{GetType().Name}:Http");
        Website = _config.GetValue<string>($"Environment:{GetType().Name}:Website");
        Selector = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:Selector"));
        ChatContainer = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:ChatContainer"));
        MessageContainer = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:MessageContainer"));
        MessageAuthor = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:MessageAuthor"));
        MessageContent = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:MessageContent"));
        CounterContainer = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:CounterContainer"));
        GameContainer = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:GameContainer"));
        CloseChatAnnouncement = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:CloseChatAnnouncement"));
        ChannelName = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:ChannelName"));
        OpenLive = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:OpenLive"));
        ReadyCheck = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:ReadyCheck"));
    }
}

public class Facebook : StreamEnvironment
{
    public Facebook() : base()
    {
        Http = _config.GetValue<string>($"Environment:{GetType().Name}:Http");
        Website = _config.GetValue<string>($"Environment:{GetType().Name}:Website");
        Selector = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:Selector"));
        ChatContainer = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:ChatContainer"));
        MessageContainer = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:MessageContainer"));
        MessageAuthor = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:MessageAuthor"));
        MessageContent = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:MessageContent"));
        CounterContainer = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:CounterContainer"));
        GameContainer = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:GameContainer"));
        CloseChatAnnouncement = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:CloseChatAnnouncement"));
        ChannelName = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:ChannelName"));
        OpenLive = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:OpenLive"));
        ReadyCheck = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:ReadyCheck"));
    }
}

public class Twitch : StreamEnvironment
{
    public Twitch() : base()
    {
        Http = _config.GetValue<string>($"Environment:{GetType().Name}:Http");
        Website = _config.GetValue<string>($"Environment:{GetType().Name}:Website");
        Selector = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:Selector"));
        ChatContainer = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:ChatContainer"));
        MessageContainer = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:MessageContainer"));
        MessageAuthor = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:MessageAuthor"));
        MessageContent = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:MessageContent"));
        CounterContainer = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:CounterContainer"));
        GameContainer = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:GameContainer"));
        CloseChatAnnouncement = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:CloseChatAnnouncement"));
        ChannelName = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:ChannelName"));
        OpenLive = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:OpenLive"));
        ReadyCheck = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:ReadyCheck"));
    }
}

public class Youtube : StreamEnvironment
{
    public Youtube() : base()
    {
        Http = _config.GetValue<string>($"Environment:{GetType().Name}:Http");
        Website = _config.GetValue<string>($"Environment:{GetType().Name}:Website");
        Selector = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:Selector"));
        ChatContainer = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:ChatContainer"));
        MessageContainer = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:MessageContainer"));
        MessageAuthor = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:MessageAuthor"));
        MessageContent = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:MessageContent"));
        CounterContainer = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:CounterContainer"));
        GameContainer = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:GameContainer"));
        CloseChatAnnouncement = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:CloseChatAnnouncement"));
        ChannelName = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:ChannelName"));
        OpenLive = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:OpenLive"));
        ReadyCheck = By.CssSelector(_config.GetValue<string>($"Environment:{GetType().Name}:ReadyCheck"));
    }
}
