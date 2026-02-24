namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Controls the lifecycle of batching groups during plan execution.
/// The executor uses this interface to register groups discovered in the plan,
/// notify the dispatcher when nodes are skipped, and abort all pending work
/// on cancellation or failure.
/// </summary>
public interface ISourceSchemaDispatcher
{
    /// <summary>
    /// Registers a batching group. The dispatcher will hold requests for the specified
    /// node IDs until all members have submitted or been skipped.
    /// </summary>
    /// <param name="groupId">The batching group identifier assigned at planning time.</param>
    /// <param name="nodeIds">The execution node IDs that belong to this group.</param>
    void RegisterGroup(int groupId, IReadOnlyList<int> nodeIds);

    /// <summary>
    /// Marks a node as skipped, removing it from its group's outstanding member count.
    /// If this was the last outstanding member, the group is dispatched with
    /// whatever requests have been submitted so far.
    /// </summary>
    /// <param name="nodeId">The ID of the execution node to skip.</param>
    void SkipNode(int nodeId);

    /// <summary>
    /// Aborts all pending batching groups, faulting any waiting callers with the
    /// specified error. Subsequent calls to <see cref="ISourceSchemaScheduler.ExecuteAsync"/>
    /// and <see cref="RegisterGroup"/> become no-ops.
    /// </summary>
    /// <param name="error">
    /// The exception to propagate to pending callers, or <c>null</c> to use a
    /// default <see cref="OperationCanceledException"/>.
    /// </param>
    void Abort(Exception? error = null);

    /// <summary>
    /// Resets the dispatcher to its initial state, clearing all groups and the aborted flag.
    /// Must be called between subscription events so that groups can be re-registered.
    /// </summary>
    void Reset();
}
