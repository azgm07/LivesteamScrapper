using OpenQA.Selenium;

namespace LivesteamScrapper.Models
{
    public interface IEnvironmentInterface
    {
        public abstract string Http { get; set; }
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
            Selector = By.TagName(string.Empty);
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
            Selector = By.CssSelector("#mount_0_0_j5 > div > div:nth-child(1) > div > div:nth-child(6) > div > div > div.rq0escxv.l9j0dhe7.du4w35lb > div > div.j83agx80.cbu4d94t.h3gjbzrl.l9j0dhe7.du4w35lb.qsy8amke > div.j83agx80.cbu4d94t.dp1hu0rb > div > div > div.hybvsw6c.j83agx80.pfnyh3mw.dp1hu0rb.l9j0dhe7.o36gj0jk > div.hybvsw6c.j83agx80.n7fi1qx3.cbu4d94t.pad24vr5.poy2od1o.iyyx5f41.ap132fyt.pphwfc2g.be9z9djy > div > div.j83agx80.cbu4d94t.buofh1pr.l9j0dhe7 > div > div > div.cwj9ozl2.ni9yibek.hv4rvrfc.dati1w0a.discj3wi > div.btwxx1t3.j83agx80 > div.rs0gx3tq.oi9244e8.buofh1pr > div > div:nth-child(1) > span > h2 > span > strong:nth-child(1) > span > a");
            ChatContainer = By.CssSelector("#mount_0_0_j5 > div > div:nth-child(1) > div > div:nth-child(6) > div > div > div.rq0escxv.l9j0dhe7.du4w35lb > div > div.j83agx80.cbu4d94t.h3gjbzrl.l9j0dhe7.du4w35lb.qsy8amke > div.j83agx80.cbu4d94t.dp1hu0rb > div > div > div.hybvsw6c.j83agx80.pfnyh3mw.dp1hu0rb.l9j0dhe7.o36gj0jk > div.hybvsw6c.j83agx80.n7fi1qx3.cbu4d94t.pad24vr5.poy2od1o.iyyx5f41.ap132fyt.pphwfc2g.be9z9djy > div > div.j83agx80.cbu4d94t.buofh1pr.l9j0dhe7 > div > div > div.j83agx80.buofh1pr.tgvbjcpo > div > div > div.j83agx80.cbu4d94t.buofh1pr.tgvbjcpo > div.stjgntxs.ni8dbmo4.tgvbjcpo.buofh1pr.j83agx80 > div > div");
            MessageContainer = By.CssSelector("#mount_0_0_j5 > div > div:nth-child(1) > div > div:nth-child(6) > div > div > div.rq0escxv.l9j0dhe7.du4w35lb > div > div.j83agx80.cbu4d94t.h3gjbzrl.l9j0dhe7.du4w35lb.qsy8amke > div.j83agx80.cbu4d94t.dp1hu0rb > div > div > div.hybvsw6c.j83agx80.pfnyh3mw.dp1hu0rb.l9j0dhe7.o36gj0jk > div.hybvsw6c.j83agx80.n7fi1qx3.cbu4d94t.pad24vr5.poy2od1o.iyyx5f41.ap132fyt.pphwfc2g.be9z9djy > div > div.j83agx80.cbu4d94t.buofh1pr.l9j0dhe7 > div > div > div.j83agx80.buofh1pr.tgvbjcpo > div > div > div.j83agx80.cbu4d94t.buofh1pr.tgvbjcpo > div.stjgntxs.ni8dbmo4.tgvbjcpo.buofh1pr.j83agx80 > div > div > div:nth-child(25)");
            MessageAuthor = By.CssSelector(@"#Y29tbWVudDoxMDExNzc5MDI5NzA5MjY4XzUwNzIwNzk3MTE1MjcxOA\=\= > div > div.rj1gh0hx.buofh1pr.ni8dbmo4.stjgntxs.hv4rvrfc > div > div.bvz0fpym.c1et5uql.q9uorilb.sf5mxxl7 > div > div > div > div > div.btwxx1t3.nc684nl6.bp9cbjyn > span > span");
            MessageContent = By.CssSelector(@"#Y29tbWVudDoxMDExNzc5MDI5NzA5MjY4XzUwNzIwNzk3MTE1MjcxOA\=\= > div > div.rj1gh0hx.buofh1pr.ni8dbmo4.stjgntxs.hv4rvrfc > div > div.bvz0fpym.c1et5uql.q9uorilb.sf5mxxl7 > div > div > div > div > div.ecm0bbzt.e5nlhep0.a8c37x1j > span > div > div");
            CounterContainer = By.XPath("/html/body/div[1]/div/div[1]/div/div[4]/div/div/div[1]/div/div[3]/div[2]/div/div/div[1]/div/div/div/div/div/div/div[2]/div/div[4]/div[2]/span[2]/text()");
            GameContainer = By.XPath("/html/body/div[1]/div/div[1]/div/div[4]/div/div/div[1]/div/div[3]/div[2]/div/div/div[2]/div[1]/div/div[1]/div/div/div[1]/div[1]/div[2]/div/div[1]/span/h2/span/strong[3]/a");
        }
    }
}