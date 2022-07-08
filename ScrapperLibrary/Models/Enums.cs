namespace ScrapperLibrary.Models;

public class Enums
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

    public enum StreamStatus
    {
        Stopped,
        Running,
        Waiting,
        NotFound
    }
}
