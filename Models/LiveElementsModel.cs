using OpenQA.Selenium;

namespace LivesteamScrapper.Models
{
    public interface ILiveElementsInterface
    {
        public abstract By CloseChatAnnouncement { get; set; }
        public abstract By ChannelName { get; set; }
    }

    public class LiveElementsModel : ILiveElementsInterface
    {
        public By CloseChatAnnouncement { get; set; }
        public By ChannelName { get; set; }

        protected readonly IConfiguration _config;

        public LiveElementsModel()
        {
            CloseChatAnnouncement = By.TagName(string.Empty);
            ChannelName = By.TagName(string.Empty);
            _config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        }

        public static LiveElementsModel GetElements(string website = "")
        {
            switch (website.ToLower())
            {
                case "booyah":
                    return new BooyahElements();
                case "facebook":
                    return new FacebookElements();
                case "twitch":
                    return new TwitchElements();
                case "youtube":
                    return new YoutubeElements();
                default:
                    return new LiveElementsModel();
            }
        }
    }
    public class BooyahElements : LiveElementsModel
    {
        public BooyahElements() : base()
        {
            CloseChatAnnouncement = By.CssSelector(_config.GetValue<string>($"LiveElements:{this.GetType().Name}:CloseChatAnnouncement"));
            ChannelName = By.CssSelector(_config.GetValue<string>($"LiveElements:{this.GetType().Name}:ChannelName"));
        }
    }

    public class FacebookElements : LiveElementsModel
    {
        public FacebookElements() : base()
        {
            CloseChatAnnouncement = By.CssSelector(_config.GetValue<string>($"LiveElements:{this.GetType().Name}:CloseChatAnnouncement"));
            ChannelName = By.CssSelector(_config.GetValue<string>($"LiveElements:{this.GetType().Name}:ChannelName"));
        }
    }

    public class TwitchElements : LiveElementsModel
    {
        public TwitchElements() : base()
        {
            CloseChatAnnouncement = By.CssSelector(_config.GetValue<string>($"LiveElements:{this.GetType().Name}:CloseChatAnnouncement"));
            ChannelName = By.CssSelector(_config.GetValue<string>($"LiveElements:{this.GetType().Name}:ChannelName"));
        }
    }

    public class YoutubeElements : LiveElementsModel
    {
        public YoutubeElements() : base()
        {
            CloseChatAnnouncement = By.CssSelector(_config.GetValue<string>($"LiveElements:{this.GetType().Name}:CloseChatAnnouncement"));
            ChannelName = By.CssSelector(_config.GetValue<string>($"LiveElements:{this.GetType().Name}:ChannelName"));
        }
    }
}