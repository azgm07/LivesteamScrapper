using OpenQA.Selenium;

namespace LivesteamScrapper.Models
{
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
    }

    public class EnvironmentModel: IEnvironmentInterface
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

        public EnvironmentModel()
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
        }

        public static EnvironmentModel GetEnvironment(string website = "")
        {
            switch (website.ToLower())
            {
                case "booyah":
                    return new Booyah();
                case "facebook":
                    return new Facebook();
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
            Website = "booyah";
            Selector = By.ClassName("message-list");
            ChatContainer = By.XPath("//*[@id=\"root\"]/div/div/div[2]/div[4]/div[1]/div/div/div[4]/div[1]/div[1]/div[1]");
            MessageContainer = By.ClassName("message");
            MessageAuthor = By.CssSelector("div > div > span.components-chatbox-user-menu > span");
            MessageContent = By.CssSelector("div > div > span.message-text");
            CounterContainer = By.CssSelector("#layout-content > div > div > div.channel-top-bar > div > div.components-profile-card-center.only-center > div.channel-infos > span > span");
            GameContainer = By.CssSelector("#layout-content > div > div > div.channel-top-bar > div > div.components-profile-card-center.only-center > div.channel-infos > div > span > a");
        }
    }

    public class Facebook : EnvironmentModel
    {
        public Facebook()
        {
            Http = "https://www.facebook.com/";
            Website = "facebook";
            Selector = By.CssSelector("div[class='f9o22wc5'] > div");
            ChatContainer = By.CssSelector("div[class='rq0escxv j83agx80 cbu4d94t eg9m0zos fh5enmmv k4urcfbm']");
            MessageContainer = By.CssSelector("div[class='tw6a2znq sj5x9vvc d1544ag0 cxgpxx05']");
            MessageAuthor = By.CssSelector("div[class='btwxx1t3 nc684nl6 bp9cbjyn'] > span > span");
            MessageContent = By.CssSelector("div[class='kvgmc6g5 cxmmr5t8 oygrvhab hcukyx3x c1et5uql'] > div");
            CounterContainer = By.CssSelector("div[class='j83agx80 rgmg9uty pmk7jnqg rnx8an3s fcg2cn6m'] > div:nth-of-type(2) > span:nth-of-type(2)");
            GameContainer = By.CssSelector("div[class='qzhwtbm6 knvmm38d'] > span > h2 > span > strong:nth-child(3) > a");
        }
    }
}