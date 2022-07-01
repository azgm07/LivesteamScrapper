using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Scrapper.Services;

public interface IProcessService
{
    public ConcurrentQueue<FuncProcess> Queue{ get; }
    public int Threads { get; set; }
    public CancellationToken CancellationToken { get; }
    public Task QueueTask { get; }
    public void RemoveProcessQueue();
    public void RemoveProcessQueue(OperationProcess operation);
    public void RemoveProcessQueue(OperationProcess operation, int index);
}
public class ProcessService : IProcessService
{
    private readonly ILogger<ProcessService> _logger;
    
    public Task QueueTask { get; private set; }

    public ConcurrentQueue<FuncProcess> Queue { get; private set; }
    public int Threads { get; set; }
    public CancellationToken CancellationToken { get; private set; }

    public ProcessService(ILogger<ProcessService> logger)
    {
        _logger = logger;
        Queue = new();

        QueueTask = Task.Run(() => ProcessQueueAsync());
    }

    public async Task ProcessQueueAsync()
    {
        try
        {
            try
            {
                await Task.Delay(5000, CancellationToken);
            }
            catch (Exception)
            {
                //Wrap task delay cancelled
            }
            while (true)
            {
                List<FuncProcess> listFunc = new();

                for (int i = 0; i < Threads; i++)
                {
                    if (Queue.TryDequeue(out FuncProcess? process) && process != null)
                    {
                        listFunc.Add(process);
                        if (process.Operation == OperationProcess.StopStream)
                        {
                            Task task = Task.Run(() => RemoveProcessQueue(OperationProcess.StartStream, process.Index));
                            await task;
                        }
                    }
                }

                List<Task> tasks = new();
                foreach (var item in listFunc)
                {
                    tasks.Add(Task.Run(item.FuncTask, CancellationToken));
                }

                await Task.WhenAll(tasks);

                //Break if shutdown was requested
                if (CancellationToken.IsCancellationRequested && Queue.IsEmpty)
                {
                    break;
                }

                await Task.Delay(1000, CancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("ProcessStreamStackAsync in WatcherService was cancelled");
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "ProcessStreamStackAsync in WatcherService finished with error");
        }
    }
    public void RemoveProcessQueue()
    {
        //Remove all
        while (!Queue.IsEmpty)
        {
            if (Queue.TryDequeue(out FuncProcess? process) && process != null)
            {
                _logger.LogInformation("Dequeued process from {index}, operation {operation}", process.Index, process.Operation);
            }
        }
    }
    public void RemoveProcessQueue(OperationProcess operation)
    {
        for (int i = 0; i < Queue.Count; i++)
        {
            if (Queue.TryDequeue(out FuncProcess? process) && process != null)
            {
                if (process.Operation != operation)
                {
                    Queue.Enqueue(process);
                }
                else
                {
                    _logger.LogInformation("Dequeued process from {index}, operation {operation}", process.Index, process.Operation);
                }
            }
        }
    }
    public void RemoveProcessQueue(OperationProcess operation, int index)
    {
        if (index >= 0)
        {
            for (int i = 0; i < Queue.Count; i++)
            {
                if (Queue.TryDequeue(out FuncProcess? process) && process != null)
                {
                    if (process.Index != index)
                    {
                        Queue.Enqueue(process);
                    }
                    else if (process.Operation != operation)
                    {
                        Queue.Enqueue(process);
                    }
                    else
                    {
                        _logger.LogInformation("Dequeued process from {index}, operation {operation}", process.Index, process.Operation);
                    }
                }
            }
        }
    }
}

public class FuncProcess
{
    public int Index { get; set; }
    public OperationProcess Operation { get; set; }
    public Func<Task> FuncTask { get; set; }

    public FuncProcess(int index, OperationProcess operation, Func<Task> funcTask)
    {
        Index = index;
        Operation = operation;
        FuncTask = funcTask;
    }
}

public enum OperationProcess
{
    StartStream,
    StopStream
}
