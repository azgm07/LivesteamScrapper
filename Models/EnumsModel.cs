namespace LivesteamScrapper.Models
{
    public class EnumsModel
    {
        public enum ScrapperMode
        {
            Off,
            Viewers,
            Chat,
            All
        }

        public enum ScrapperStatus
        {
            Stopped,
            Running,
            Waiting,
            NotFound
        }
    }
}