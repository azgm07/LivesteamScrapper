using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrapperLibrary.Models
{
    public sealed class TimerPlus : System.Timers.Timer, IDisposable
    {
        private DateTime m_dueTime;
        private bool _elapsedOnce;
        public bool ElapsedOnce
        {
            get
            {
                return _elapsedOnce;
            }
            set
            {
                _elapsedOnce = value;
                if (value)
                {
                    ElapsedOnceEvent?.Invoke();
                }
            }
        }
        public TimerPlus(int timer) : base(timer) => Elapsed += ElapsedAction;

        public delegate void ElapsedOnceEventHandler();

        public event ElapsedOnceEventHandler? ElapsedOnceEvent;

        public new void Dispose()
        {
            Elapsed -= ElapsedAction;
            base.Dispose();
        }

        public double TimeLeft
        {
            get
            {
                if (Enabled)
                {
                    return (m_dueTime - DateTime.Now).TotalMilliseconds;
                }
                else
                {
                    return 0;
                }
            }
        }
        public new void Start()
        {
            m_dueTime = DateTime.Now.AddMilliseconds(Interval);
            ElapsedOnce = false;
            base.Start();
        }

        public new void Stop()
        {
            ElapsedOnce = false;
            base.Stop();
        }

        private void ElapsedAction(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (AutoReset)
            {
                ElapsedOnce = true;
                m_dueTime = DateTime.Now.AddMilliseconds(Interval);
            }
        }
    }

}
