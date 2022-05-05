using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;

namespace LivesteamScrapper.Controllers
{
    public class TimeController : Controller
    {
        public DateTime StartTime { get; set; }
        public DateTime StopTime { get; set; }
        public List<DateTime> LapTime { get; set; }
        public string From { get; private set; }
        public System.Timers.Timer Timer { get; private set; }

        private readonly ILogger<Controller> _logger;

        public TimeController(ILogger<Controller> logger, string from)
        {
            _logger = logger;
            StartTime = new DateTime();
            StopTime = new DateTime();
            LapTime = new List<DateTime>();
            Timer = new System.Timers.Timer();
            From = from;
        }

        //Start
        public DateTime Start(string moreInfo = "")
        {
            LapTime = new List<DateTime>();
            Timer.Start();
            StartTime = DateTime.Now;
            
            ConsoleController.ShowTimeLog(EnumsModel.TimerLog.Start, this, moreInfo);
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
                ConsoleController.ShowTimeLog(EnumsModel.TimerLog.Lap, this, moreInfo);
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
                ConsoleController.ShowTimeLog(EnumsModel.TimerLog.Stop, this, moreInfo);
                return StopTime;
            }
        }
    }
}