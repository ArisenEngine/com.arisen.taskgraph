using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arisen.DAG;

namespace ArisenEngine.Threading;

/// <summary>
/// The primary entry point for scheduling and executing DAG-based tasks.
/// </summary>
public sealed class TaskGraph : IDisposable
{
    private readonly TaskWorker[] m_Workers;
    private readonly Graph<TaskNode> m_Graph = new();
    private readonly GraphCompiler<TaskNode> m_Compiler = new();

    public TaskGraph(int workerCount = 0)
    {
        // Default to processor count if not specified
        if (workerCount <= 0)
        {
            workerCount = Environment.ProcessorCount;
        }

        m_Workers = new TaskWorker[workerCount];
        for (int i = 0; i < workerCount; i++)
        {
            m_Workers[i] = new TaskWorker(i);
        }
    }

    /// <summary>
    /// Adds a task to the current graph.
    /// </summary>
    public TaskNode AddTask(TaskNode task)
    {
        return m_Graph.AddNode(task);
    }

    /// <summary>
    /// Adds a dependency between two tasks. (src must complete before dst starts)
    /// </summary>
    public void AddDependency(TaskNode src, TaskNode dst)
    {
        // Use port index 0 as standard for task dependencies
        m_Graph.Connect(src.Id, 0, dst.Id, 0);
    }

    /// <summary>
    /// Compiles and executes the current graph.
    /// This method blocks until all tasks in the graph have completed.
    /// </summary>
    public void Execute()
    {
        var compiled = m_Compiler.Compile(m_Graph);
        
        // Execute layer by layer to respect dependencies
        foreach (var layer in compiled.ParallelLayers)
        {
            if (layer.Count == 0) continue;

            using var countdown = new CountdownEvent(layer.Count);

            foreach (var node in layer)
            {
                // Wrap task to notify countdown on completion
                var taskWrapper = new ActionTask(() =>
                {
                    try { node.Execute(); }
                    finally { countdown.Signal(); }
                }, node.Name);

                // Dispatch to workers in a round-robin fashion
                int workerIndex = (int)(node.Id % (uint)m_Workers.Length);
                m_Workers[workerIndex].Enqueue(taskWrapper);
            }

            // Wait for the entire layer to finish before moving to the next
            countdown.Wait();
        }
    }

    public void Dispose()
    {
        foreach (var worker in m_Workers)
        {
            worker.Dispose();
        }
    }
}
