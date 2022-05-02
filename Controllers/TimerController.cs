using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;

namespace LivesteamScrapper.Controllers
{
    public class TimerController : Controller
    {
        public DateTime StartTime { get; set; }
        public DateTime StopTime { get; set; }
        public DateTime LapTime { get; set; }

        public TimeSpan LapTimer { get; set; }

        public bool HasStarted { get; private set; }
        public int LapCount { get; private set; }
        public string From { get; private set; }

        private readonly ILogger<Controller> _logger;

        public TimerController(ILogger<Controller> logger, string from)
        {
            _logger = logger;
            StartTime = new DateTime();
            StopTime = new DateTime();
            LapTime = new DateTime();
            HasStarted = false;
            LapCount = 1;
            From = from;
        }

        //Start timer
        public DateTime StartTimer()
        {
            if (!HasStarted)
            {
                StartTime = DateTime.Now;
                LapCount = 1;
                HasStarted = true;
            }
            ConsoleController.ShowTimerLog(EnumsModel.TimerLog.Start, this);
            return StartTime;
        }

        //Get the current TimeSpan between start and now
        public TimeSpan? GetTimerTotal()
        {
            if (!HasStarted)
            {
                return null;
            }
            else
            {
                TimeSpan timer = DateTime.Now - StartTime;
                return timer;
            }
        }

        //Get the current TimeSpan between last lap and now
        public TimeSpan? GetTimerLap()
        {
            if (!HasStarted)
            {
                return null;
            }
            else
            {
                if (LapCount == 1)
                {
                    LapCount = 2;
                    LapTimer = DateTime.Now - StartTime;
                    LapTime = DateTime.Now;
                }
                else
                {
                    LapCount += 1;
                    LapTimer = DateTime.Now - StartTime;
                    LapTime = DateTime.Now;
                }
                ConsoleController.ShowTimerLog(EnumsModel.TimerLog.Lap, this);
                return LapTimer;
            }
        }

        //Stop timer
        public DateTime? StopTimer()
        {
            if (!HasStarted)
            {
                return null;
            }
            else
            {
                StopTime = DateTime.Now;
                HasStarted = false;
                ConsoleController.ShowTimerLog(EnumsModel.TimerLog.Stop, this);
                return StopTime;
            }
        }
    }
}