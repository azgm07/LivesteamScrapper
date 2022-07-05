using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Scrapper.Services;

public interface IProcessService
{
    public ConcurrentQueue<FuncProcess> StopQueue{ get; }
    public ConcurrentQueue<FuncProcess> StartQueue{ get; }
    public ConcurrentQueue<FuncProcess> RunQueue{ get; }
    public int Threads { get; set; }
    public void RemoveProcessQueue(ConcurrentQueue<FuncProcess> queue);
    public void RemoveProcessQueue(ConcurrentQueue<FuncProcess> queue, OperationProcess operation);
    public void RemoveProcessQueue(ConcurrentQueue<FuncProcess> queue, OperationProcess operation, int index);
    public Task ProcessQueueAsync(CancellationToken token);
}
public class ProcessService : IProcessService
{
    private readonly ILogger<ProcessService> _logger;
    
    public ConcurrentQueue<FuncProcess> StartQueue { get; private set; }
    public ConcurrentQueue<FuncProcess> StopQueue { get; private set; }
    public ConcurrentQueue<FuncProcess> RunQueue { get; private set; }
    public int Threads { get; set; }

    public ProcessService(ILogger<ProcessService> logger)
    {
        _logger = logger;
        StartQueue = new();
        StopQueue = new();
        RunQueue = new();
    }

    public async Task ProcessQueueAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(5000, token);

            while (true)
            {
                List<FuncProcess> listFunc = new();

                for (int i = 0; i < Threads; i++)
                {
                    if (StopQueue.TryDequeue(out FuncProcess? processStop) && processStop != null)
                    {
                        listFunc.Add(processStop);
                        List<Task> tasksList = new()
                        {
                            Task.Run(() => RemoveProcessQueue(StartQueue, processStop.Index), CancellationToken.None),
                            Task.Run(() => RemoveProcessQueue(RunQueue, processStop.Index), CancellationToken.None)
                        };
                        for (int j = 0; j < listFunc.Count; j++)
                        {
                            if (listFunc[j].Index == processStop.Index)
                            {
                                listFunc.RemoveAt(j);
                            }
                        }
                        await Task.WhenAll(tasksList);
                    }
                    else if (StartQueue.TryDequeue(out FuncProcess? processStart) && processStart != null)
                    {
                        listFunc.Add(processStart);
                    }
                    else if (RunQueue.TryDequeue(out FuncProcess? processRun) && processRun != null)
                    {
                        listFunc.Add(processRun);
                    }
                }

                List<Task> tasks = new();
                foreach (var item in listFunc)
                {
                    tasks.Add(Task.Run(item.FuncTask, token));
                }

                await Task.WhenAll(tasks);

                //Break if shutdown was requested
                if (token.IsCancellationRequested && StartQueue.IsEmpty && StopQueue.IsEmpty && RunQueue.IsEmpty)
                {
                    break;
                }

                await Task.Delay(1000, token);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("ProcessQueueAsync in ProcessService was cancelled");
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "ProcessQueueAsync in ProcessService finished with error");
        }
    }
    public void RemoveProcessQueue(ConcurrentQueue<FuncProcess> queue)
    {
        //Remove all
        while (!queue.IsEmpty)
        {
            if (queue.TryDequeue(out FuncProcess? process) && process != null)
            {
                _logger.LogInformation("Dequeued process from {index}, operation {operation}", process.Index, process.Operation);
            }
        }
    }
    public void RemoveProcessQueue(ConcurrentQueue<FuncProcess> queue, OperationProcess operation)
    {
        for (int i = 0; i < queue.Count; i++)
        {
            if (queue.TryDequeue(out FuncProcess? process) && process != null)
            {
                if (process.Operation != operation)
                {
                    queue.Enqueue(process);
                }
                else
                {
                    _logger.LogInformation("Dequeued process from {index}, operation {operation}", process.Index, process.Operation);
                }
            }
        }
    }
    public void RemoveProcessQueue(ConcurrentQueue<FuncProcess> queue, int index)
    {
        for (int i = 0; i < queue.Count; i++)
        {
            if (queue.TryDequeue(out FuncProcess? process) && process != null)
            {
                if (process.Index != index)
                {
                    queue.Enqueue(process);
                }
                else
                {
                    _logger.LogInformation("Dequeued process from {index}, operation {operation}", process.Index, process.Operation);
                }
            }
        }
    }
    public void RemoveProcessQueue(ConcurrentQueue<FuncProcess> queue, OperationProcess operation, int index)
    {
        if (index >= 0)
        {
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue.TryDequeue(out FuncProcess? process) && process != null)
                {
                    if (process.Index != index)
                    {
                        queue.Enqueue(process);
                    }
                    else if (process.Operation != operation)
                    {
                        queue.Enqueue(process);
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
    StopStream,
    RunScrapper
}
