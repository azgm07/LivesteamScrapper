namespace Scrapper.Models;

public class EnumsModel
{
    public enum ScrapperMode
    {
        Delayed,
        Precise
    }

    public enum ScrapperStatus
    {
        Stopped,
        Running,
        Waiting,
        NotFound
    }
}
