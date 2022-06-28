using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrapper.Services
{
    public abstract class BackgroundService : IHostedService, IDisposable
    {
        protected ILogger<BackgroundService> _logger;
        protected Task _executingTask = Task.CompletedTask;
        protected readonly CancellationTokenSource _stoppingCts = new();

        protected BackgroundService(ILogger<BackgroundService> logger)
        {
            _logger = logger;
        }

        public abstract Task ExecuteAsync(CancellationToken stoppingToken);

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            // Store the task we're executing
            _executingTask = ExecuteAsync(_stoppingCts.Token);

            // If the task is completed then return it,
            // this will bubble cancellation and failure to the caller
            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executingTask == null)
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
                await Task.Delay(Timeout.Infinite, cancellationToken);
                Task task = _executingTask;
                await task;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "StopAsync Unhandled exception!");
            }

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _stoppingCts.Cancel();
        }
    }
}
