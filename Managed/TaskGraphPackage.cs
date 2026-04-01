using ArisenKernel.Packages;
using ArisenKernel.Services;
using System.Diagnostics;

namespace ArisenEngine.Threading;

/// <summary>
/// Registers the TaskGraph (Job System) in the Arisen Service Registry.
/// </summary>
public class TaskGraphPackage : IPackageEntry
{
    private TaskGraph? m_TaskGraph;

    public void OnLoad(IServiceRegistry registry)
    {
        // One worker per logical thread minus one (for the main thread)
        int workerCount = System.Environment.ProcessorCount - 1;
        if (workerCount < 1) workerCount = 1;

        m_TaskGraph = new TaskGraph(workerCount);
        registry.RegisterService<ITaskGraph>(m_TaskGraph);
        
        Debug.WriteLine($"[TaskGraphPackage] Started with {workerCount} workers.");
    }

    public void OnUnload(IServiceRegistry registry)
    {
        m_TaskGraph?.Dispose();
        m_TaskGraph = null;
    }
}

/// <summary>
/// Minimal interface for the TaskGraph to allow decoupling.
/// </summary>
public interface ITaskGraph
{
    TaskNode AddTask(TaskNode task);
    void AddDependency(TaskNode src, TaskNode dst);
    void Execute();
}

/// <summary>
/// Wrap the existing TaskGraph to implement the interface.
/// </summary>
public partial class TaskGraph : ITaskGraph { }
