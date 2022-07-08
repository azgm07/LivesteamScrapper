using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ScrapperLibrary.Models.Enums;
using ScrapperLibrary.Interfaces;

namespace ScrapperLibrary.Models
{
    public sealed class StreamObject : IDisposable
    {
        public string Website { get; set; }
        public string Channel { get; set; }
        public StreamEnvironment Environment { get; set; }
        private readonly IServiceScope _scope;
        public IScrapperService Scrapper
        {
            get
            {
                IScrapperService service = (IScrapperService)_scope.ServiceProvider.GetRequiredService(typeof(IScrapperService));
                return service;

            }
        }
        private ScrapperStatus _status;
        public ScrapperStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                ChangeScrapperStatusEvent?.Invoke(this);
            }
        }
        public TimerPlus WaitTimer { get; set; }

        public delegate void ChangeScrapperStatusEventHandler(StreamObject stream);

        public static event ChangeScrapperStatusEventHandler? ChangeScrapperStatusEvent;

        public delegate void ElapsedOnceEventHandler(StreamObject stream);

        public static event ElapsedOnceEventHandler? ElapsedOnceEvent;

        public StreamObject(string website, string channel, StreamEnvironment environment, IServiceScopeFactory scopeService, int timer)
        {
            Website = website;
            Channel = channel;
            Environment = environment;
            _scope = scopeService.CreateScope();
            Status = ScrapperStatus.Stopped;
            WaitTimer = new(timer * 1000)
            {
                AutoReset = true
            };
            WaitTimer.ElapsedOnceEvent += WaitTimer_ElapsedOnceEvent;
            Scrapper.StatusChangeEvent += Scrapper_StatusChangeEvent;
        }

        private void Scrapper_StatusChangeEvent()
        {
            ChangeScrapperStatusEvent?.Invoke(this);
        }

        private void WaitTimer_ElapsedOnceEvent()
        {
            ElapsedOnceEvent?.Invoke(this);
        }

        public void Dispose()
        {
            WaitTimer.ElapsedOnceEvent -= WaitTimer_ElapsedOnceEvent;
            Scrapper.StatusChangeEvent -= Scrapper_StatusChangeEvent;
            Status = ScrapperStatus.Stopped;
            Scrapper.Stop();
            Scrapper.Dispose();
            WaitTimer?.Dispose();
        }
    }
}
