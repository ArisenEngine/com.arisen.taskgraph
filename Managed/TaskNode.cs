using Arisen.DAG;
using System;

namespace ArisenEngine.Threading;

/// <summary>
/// Represents a single execution unit in the Arisen TaskGraph.
/// </summary>
public abstract class TaskNode : GraphNode
{
    /// <summary>
    /// The core execution logic for the task.
    /// This will be called by a WorkerThread.
    /// </summary>
    public abstract void Execute();

    /// <summary>
    /// Optional callback for post-execution cleanup or status reporting.
    /// </summary>
    public virtual void OnCompleted() { }
}

/// <summary>
/// A delegate-based task for quick ad-hoc job creation.
/// </summary>
public sealed class ActionTask : TaskNode
{
    private readonly Action m_Action;

    public ActionTask(Action action, string name = "AnonymousTask")
    {
        m_Action = action;
        Name = name;
    }

    public override void Execute()
    {
        m_Action();
    }
}
