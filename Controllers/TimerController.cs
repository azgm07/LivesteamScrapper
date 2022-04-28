using Microsoft.AspNetCore.Mvc;

namespace LivesteamScrapper.Controllers
{
    public class TimerController : Controller
    {
        private DateTime startTime;
        private DateTime stopTime;
        private DateTime lapTime;

        public bool HasStarted { get; private set; }
        public int LapCount { get; private set; }

        private readonly ILogger<Controller> _logger;

        public TimerController(ILogger<Controller> logger)
        {
            _logger = logger;
            startTime = new DateTime();
            stopTime = new DateTime();
            lapTime = new DateTime();
            HasStarted = false;
            LapCount = 1;
        }

        //Start timer
        public DateTime StartTimer()
        {
            if (!HasStarted)
            {
                startTime = DateTime.Now;
                LapCount = 1;
                HasStarted = true;
            }
            return startTime;
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
                TimeSpan timer = DateTime.Now - startTime;
                return timer;
            }
        }

        //Get the current TimeSpan between last lap and now
        public TimeSpan? GetTimerLap()
        {
            TimeSpan lapTimer;
            if (!HasStarted)
            {
                return null;
            }
            else
            {
                if (LapCount == 1)
                {
                    LapCount = 2;
                    lapTimer = DateTime.Now - startTime;
                    lapTime = DateTime.Now;
                }
                else
                {
                    LapCount += 1;
                    lapTimer = DateTime.Now - lapTime;
                    lapTime = DateTime.Now;
                }

                return lapTimer;
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
                stopTime = DateTime.Now;
                HasStarted = false;
                return stopTime;
            }
        }
    }
}