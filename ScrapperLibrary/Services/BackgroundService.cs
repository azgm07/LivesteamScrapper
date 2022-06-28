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
        protected readonly IHostApplicationLifetime _appLifetime;

        protected BackgroundService(ILogger<BackgroundService> logger, IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _appLifetime = appLifetime;
        }

        public abstract Task ExecuteAsync(CancellationToken stoppingToken);

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(OnStarted);
            _appLifetime.ApplicationStopping.Register(OnStopping);
            _appLifetime.ApplicationStopped.Register(OnStopped);

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

        protected virtual void OnStarted()
        {
            // Perform post-startup activities here
        }

        protected virtual void OnStopping()
        {
            // Perform on-stopping activities here
        }

        protected virtual void OnStopped()
        {
            // Perform post-stopped activities here
        }
    }
}
