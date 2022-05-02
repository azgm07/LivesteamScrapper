using OpenQA.Selenium;

namespace LivesteamScrapper.Models
{
    public interface IEnvironmentInterface
    {
        public abstract string Http { get; set; }
        public abstract string Selector { get; set; }
        public abstract By ChatContainer { get; set; }
        public abstract By MessageContainer { get; set; }
        public abstract By MessageAuthor { get; set; }
        public abstract By MessageContent { get; set; }
        public abstract By CounterContainer { get; set; }
        public abstract By GameContainer { get; set; }
    }

    public class EnvironmentModel: IEnvironmentInterface
    {
        public string Http { get; set; }
        public string Selector { get; set; }
        public By ChatContainer { get; set; }
        public By MessageContainer { get; set; }
        public By MessageAuthor { get; set; }
        public By MessageContent { get; set; }
        public By CounterContainer { get; set; }
        public By GameContainer { get; set; }

        public EnvironmentModel()
        {
            Http = string.Empty;
            Selector = string.Empty;
            ChatContainer = By.TagName(string.Empty);
            MessageContainer = By.TagName(string.Empty);
            MessageAuthor = By.TagName(string.Empty);
            MessageContent = By.TagName(string.Empty);
            CounterContainer = By.TagName(string.Empty);
            GameContainer = By.TagName(string.Empty);
        }

        public static EnvironmentModel CreateEnvironment(string website = "")
        {
            switch (website.ToLower())
            {
                case "booyah":
                    return new Booyah();
                default:
                    return new EnvironmentModel();
            }
        }
    }
    public class Booyah : EnvironmentModel
    {
        public Booyah()
        {
            Http = "https://booyah.live/";
            Selector = "message-list";
            ChatContainer = By.XPath("//*[@id=\"root\"]/div/div/div[2]/div[4]/div[1]/div/div/div[4]/div[1]/div[1]/div[1]");
            MessageContainer = By.ClassName("message");
            MessageAuthor = By.CssSelector("div > div > span.components-chatbox-user-menu > span");
            MessageContent = By.CssSelector("div > div > span.message-text");
            CounterContainer = By.CssSelector("#layout-content > div > div > div.channel-top-bar > div > div.components-profile-card-center.only-center > div.channel-infos > span > span");
            GameContainer = By.CssSelector("#layout-content > div > div > div.channel-top-bar > div > div.components-profile-card-center.only-center > div.channel-infos > div > span > a");
        }
    }
}