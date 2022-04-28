using LivesteamScrapper.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using HtmlAgilityPack;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.IO;
using PuppeteerSharp;

namespace LivesteamScrapper.Controllers
{
    public class TimerController : Controller
    {
        private DateTime startTime;
        private DateTime stopTime;
        private DateTime lapTime;

        public bool hasStarted;
        public int lapCount;

        private readonly ILogger<Controller> _logger;

        public TimerController(ILogger<Controller> logger)
        {
            _logger = logger;
            startTime = new DateTime();
            stopTime = new DateTime();
            lapTime = new DateTime();
            hasStarted = false;
            lapCount = 0;
        }

        //Start timer
        public DateTime StartTimer()
        {
            if(!hasStarted)
            {
                startTime = DateTime.Now;
                lapCount = 0;
                hasStarted = true;
            }
            return startTime;
        }

        //Get the current TimeSpan between start and now
        public TimeSpan? GetTimerTotal()
        {
            if(!hasStarted)
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
            if(!hasStarted)
            {
                return null;
            }
            else
            {
                if(lapCount == 0)
                {
                    lapCount = 1;
                    lapTimer = DateTime.Now - startTime;
                    lapTime = DateTime.Now;
                }
                else
                {
                    lapCount += 1;
                    lapTimer = DateTime.Now - lapTime;
                    lapTime = DateTime.Now;
                }

                return lapTimer;
            }
        }

        //Stop timer
        public DateTime? StopTimer()
        {
            if(!hasStarted)
            {
                return null;
            }
            else
            {
                stopTime = DateTime.Now;
                hasStarted = false;
                return stopTime;
            }
        }
    }
}