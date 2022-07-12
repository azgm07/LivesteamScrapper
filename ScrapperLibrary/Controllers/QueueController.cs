using Microsoft.Extensions.Logging;
using ScrapperLibrary.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrapperLibrary.Controllers
{
    internal class QueueController
    {
        public int Threads { get; set; }

        private readonly ILogger<QueueController> _logger;

        public ConcurrentQueue<QueueFunc> ProcessQueue { get; private set; }

        public QueueController(ILogger<QueueController> logger, int threads = 5)
        {
            ProcessQueue = new();
            _logger = logger;
            Threads = threads;
        }

        public async Task RunQueueAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(5000, token);

                while (true)
                {
                    List<QueueFunc> listFunc = new();

                    for (int i = 0; i < Threads; i++)
                    {
                        if (ProcessQueue.TryDequeue(out QueueFunc? process) && process != null)
                        {
                            listFunc.Add(process);
                        }
                    }

                    List<Task> tasks = new();
                    foreach (var item in listFunc)
                    {
                        tasks.Add(Task.Run(item.FuncTask, token));
                    }

                    await Task.WhenAll(tasks);

                    //Break if shutdown was requested
                    if (token.IsCancellationRequested && ProcessQueue.IsEmpty)
                    {
                        break;
                    }

                    await Task.Delay(1000, token);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("RunQueueAsync in QueueController was cancelled");
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "RunQueueAsync in QueueController finished with error");
            }
        }
        public void RemoveFromQueue(ConcurrentQueue<QueueFunc> queue)
        {
            //Remove all
            while (!queue.IsEmpty)
            {
                if (queue.TryDequeue(out QueueFunc? process) && process != null)
                {
                    _logger.LogInformation("Dequeued process from {index}", process.Index);
                }
            }
        }

        public void RemoveFromQueue(ConcurrentQueue<QueueFunc> queue, int index)
        {
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue.TryDequeue(out QueueFunc? process) && process != null)
                {
                    if (process.Index != index)
                    {
                        queue.Enqueue(process);
                    }
                    else
                    {
                        _logger.LogInformation("Dequeued process from {index}", process.Index);
                    }
                }
            }
        }
    }
}
