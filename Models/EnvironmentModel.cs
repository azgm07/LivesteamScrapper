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

        protected readonly IConfiguration _config;

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
            _config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        }

        public static EnvironmentModel GetEnvironment(string website = "")
        {
            return website.ToLower() switch
            {
                "booyah" => new Booyah(),
                "facebook" => new Facebook(),
                "twitch" => new Twitch(),
                "youtube" => new Youtube(),
                _ => new EnvironmentModel(),
            };
        }
    }
    public class Booyah : EnvironmentModel
    {
        public Booyah() : base()
        {
            Http = _config.GetValue<string>($"Environment:{this.GetType().Name}:Http");
            Website = _config.GetValue<string>($"Environment:{this.GetType().Name}:Website");
            Selector = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:Selector"));
            ChatContainer = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:ChatContainer"));
            MessageContainer = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:MessageContainer"));
            MessageAuthor = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:MessageAuthor"));
            MessageContent = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:MessageContent"));
            CounterContainer = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:CounterContainer"));
            GameContainer = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:GameContainer"));
        }
    }

    public class Facebook : EnvironmentModel
    {
        public Facebook() : base()
        {
            Http = _config.GetValue<string>($"Environment:{this.GetType().Name}:Http");
            Website = _config.GetValue<string>($"Environment:{this.GetType().Name}:Website");
            Selector = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:Selector"));
            ChatContainer = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:ChatContainer"));
            MessageContainer = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:MessageContainer"));
            MessageAuthor = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:MessageAuthor"));
            MessageContent = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:MessageContent"));
            CounterContainer = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:CounterContainer"));
            GameContainer = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:GameContainer"));
        }
    }

    public class Twitch : EnvironmentModel
    {
        public Twitch() : base()
        {
            Http = _config.GetValue<string>($"Environment:{this.GetType().Name}:Http");
            Website = _config.GetValue<string>($"Environment:{this.GetType().Name}:Website");
            Selector = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:Selector"));
            ChatContainer = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:ChatContainer"));
            MessageContainer = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:MessageContainer"));
            MessageAuthor = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:MessageAuthor"));
            MessageContent = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:MessageContent"));
            CounterContainer = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:CounterContainer"));
            GameContainer = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:GameContainer"));
        }
    }

    public class Youtube : EnvironmentModel
    {
        public Youtube() : base()
        {
            Http = _config.GetValue<string>($"Environment:{this.GetType().Name}:Http");
            Website = _config.GetValue<string>($"Environment:{this.GetType().Name}:Website");
            Selector = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:Selector"));
            ChatContainer = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:ChatContainer"));
            MessageContainer = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:MessageContainer"));
            MessageAuthor = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:MessageAuthor"));
            MessageContent = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:MessageContent"));
            CounterContainer = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:CounterContainer"));
            GameContainer = By.CssSelector(_config.GetValue<string>($"Environment:{this.GetType().Name}:GameContainer"));
        }
    }
}