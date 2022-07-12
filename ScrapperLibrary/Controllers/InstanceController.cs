using ScrapperLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ScrapperLibrary.Models.Enums;

namespace ScrapperLibrary.Controllers
{
    public sealed class InstanceController : IDisposable
    {
        public StreamStatus Status { get; private set; }
        public TrackerController Tracker { get; set; }
        public TimerPlus WaitTimer { get; private set; }
        public TimerPlus RunTimer { get; private set; }
        public readonly int Index;

        public delegate void CallRunEventHandler(InstanceController instance);

        public event CallRunEventHandler? CallRunEvent;

        public InstanceController(int index, StreamStatus status, TrackerController tracker, int delayToRun, int secondsToWait)
        {
            Index = index;
            Status = status;
            Tracker = tracker;

            WaitTimer = new(secondsToWait * 1000)
            {
                AutoReset = true
            };
            WaitTimer.ElapsedOnceEvent += WaitTimer_ElapsedOnceEvent;

            RunTimer = new(delayToRun * 1000)
            {
                AutoReset = true
            };
            RunTimer.ElapsedOnceEvent += RunTimer_ElapsedOnceEvent;

            tracker.NewInfoEvent += Tracker_NewInfoEvent;
        }

        public void Start()
        {
            Status = StreamStatus.Running;
            CallRunEvent?.Invoke(this);
            RunTimer.Start();
        }

        public void Stop()
        {
            Status = StreamStatus.Stopped;
            RunTimer.Stop();
        }

        private void WaitTimer_ElapsedOnceEvent()
        {
            if (Status == StreamStatus.Waiting)
            {
                WaitTimer.Stop();
                RunTimer.Start();
                Status = StreamStatus.Running;
            }
            else
            {
                WaitTimer.Stop();
            }
        }

        private void RunTimer_ElapsedOnceEvent()
        {
            if (Status == StreamStatus.Running)
            {
                CallRunEvent?.Invoke(this);
                RunTimer.Start();
            }
            else
            {
                Tracker.ResetTracker();
                RunTimer.Stop();
            }
        }

        private void Tracker_NewInfoEvent(bool result)
        {
            if (!result && Status == StreamStatus.Running)
            {
                Status = StreamStatus.Waiting;
                WaitTimer.Start();
                Tracker.ResetTracker();
            }
        }

        public void Dispose()
        {
            WaitTimer.ElapsedOnceEvent -= WaitTimer_ElapsedOnceEvent;
            WaitTimer.Dispose();
            RunTimer.ElapsedOnceEvent -= RunTimer_ElapsedOnceEvent;
            RunTimer.Dispose();
        }
    }
}
