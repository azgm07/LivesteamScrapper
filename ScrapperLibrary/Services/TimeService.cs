using Microsoft.Extensions.Logging;

namespace Scrapper.Services;

public interface ITimeService
{
    public DateTime StartTime { get; }
    public DateTime StopTime { get; }
    public List<DateTime> LapTime { get; }
    public string From { get; }
    public System.Timers.Timer Timer { get; }

    public DateTime Start(string from, string moreInfo = "");
    public TimeSpan? GetTimerTotal();
    public TimeSpan? Lap(string moreInfo = "");
    public DateTime? Stop(string moreInfo = "");
}

public class TimeService : ITimeService
{
    public DateTime StartTime { get; private set; }
    public DateTime StopTime { get; private set; }
    public List<DateTime> LapTime { get; private set; }
    public string From { get; private set; }
    public System.Timers.Timer Timer { get; private set; }

    private readonly ILogger<TimeService> _logger;

    public TimeService(ILogger<TimeService> logger)
    {
        _logger = logger;
        From = String.Empty;
        StartTime = new DateTime();
        StopTime = new DateTime();
        LapTime = new List<DateTime>();
        Timer = new System.Timers.Timer();
    }

    //Start
    public DateTime Start(string from, string moreInfo = "")
    {
        From = from;
        LapTime = new List<DateTime>();
        Timer.Start();
        StartTime = DateTime.Now;

        _logger.LogInformation("Start time on {From} : {StartTime} | {MoreInfo}", From, StartTime, moreInfo);
        return StartTime;
    }

    //Get the current TimeSpan between start and now
    public TimeSpan? GetTimerTotal()
    {
        if (!Timer.Enabled)
        {
            return null;
        }
        else
        {
            TimeSpan time = DateTime.Now - StartTime;
            return time;
        }
    }

    //Get the current TimeSpan between last lap and now
    public TimeSpan? Lap(string moreInfo = "")
    {
        if (!Timer.Enabled)
        {
            return null;
        }
        else
        {
            DateTime now = DateTime.Now;
            LapTime.Add(now);
            _logger.LogInformation("Lap count on {From} : {LapTime} | {MoreInfo}", From, LapTime, moreInfo);
            return (now - StartTime);
        }
    }

    //Stop timer
    public DateTime? Stop(string moreInfo = "")
    {
        if (!Timer.Enabled)
        {
            return null;
        }
        else
        {
            StopTime = DateTime.Now;
            Timer.Stop();
            _logger.LogInformation("Stop time on {From} : {StopTime} | {MoreInfo}", From, StopTime, moreInfo);
            return StopTime;
        }
    }
}
