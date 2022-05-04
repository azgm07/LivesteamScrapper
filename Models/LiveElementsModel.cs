using OpenQA.Selenium;

namespace LivesteamScrapper.Models
{
    public interface ILiveElementsInterface
    {
        public abstract By CloseChatAnnouncement { get; set; }
    }

    public class LiveElementsModel : ILiveElementsInterface
    {
        public By CloseChatAnnouncement { get; set; }

        public LiveElementsModel()
        {
            CloseChatAnnouncement = By.TagName(string.Empty);
        }

        public static LiveElementsModel GetElements(string website = "")
        {
            switch (website.ToLower())
            {
                case "booyah":
                    return new BooyahElements();
                case "facebook":
                    return new FacebookElements();
                default:
                    return new LiveElementsModel();
            }
        }
    }
    public class BooyahElements : LiveElementsModel
    {
        public BooyahElements()
        {
            CloseChatAnnouncement = By.TagName(string.Empty);
        }
    }

    public class FacebookElements : LiveElementsModel
    {
        public FacebookElements()
        {
            CloseChatAnnouncement = By.CssSelector("div[class='f9o22wc5'] > div");
        }
    }
}