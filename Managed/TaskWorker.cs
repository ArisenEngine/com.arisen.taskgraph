using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ArisenEngine.Threading;

/// <summary>
/// A persistent worker thread inside the Arisen TaskGraph.
/// </summary>
public sealed class TaskWorker : IDisposable
{
    private readonly Thread m_Thread;
    private readonly BlockingCollection<TaskNode> m_WorkQueue = new();
    private volatile bool m_Running = true;
    private readonly int m_WorkerId;

    public int WorkerId => m_WorkerId;

    public TaskWorker(int workerId)
    {
        m_WorkerId = workerId;
        m_Thread = new Thread(WorkerLoop)
        {
            Name = $"ArisenWorker-{workerId}",
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal
        };
        m_Thread.Start();
    }

    /// <summary>
    /// Enqueues a task for execution in this worker.
    /// </summary>
    public void Enqueue(TaskNode task)
    {
        m_WorkQueue.Add(task);
    }

    private void WorkerLoop()
    {
        while (m_Running)
        {
            try
            {
                if (m_WorkQueue.TryTake(out var task, Timeout.Infinite))
                {
                    task.Execute();
                    task.OnCompleted();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // TODO: Integration with Arisen Kernel Logger
                Console.WriteLine($"[ArisenWorker-{m_WorkerId}] Error executing task: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        m_Running = false;
        m_WorkQueue.CompleteAdding();
        if (m_Thread.IsAlive)
        {
            m_Thread.Join(500); // Wait for graceful exit
        }
    }
}
