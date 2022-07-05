using Scrapper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrapperLibrary.Interfaces
{
    public interface IScrapperService : IDisposable
    {
        CancellationTokenSource Cts { get; }
        string CurrentGame { get; }
        bool IsScrapping { get; }
        string Livestream { get; }
        int MaxFails { get; }
        int ViewersCount { get; }
        string Website { get; }
        int DelayInSeconds { get; }

        Task RunTestAsync(EnvironmentModel environment, string livestream, int minutes);
        Task<bool> RunScrapperAsync(EnvironmentModel environment, string livestream, int index = -1);
        void Stop();

        public delegate void StatusChangeEventHandler();
        public event StatusChangeEventHandler? StatusChangeEvent;
    }
}
