using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// Represents a deprioritized part of the query that will be executed after
    /// the main execution has finished.
    /// </summary>
    internal interface IDeferredExecutionTask
    {
        /// <summary>
        /// Executes the deferred execution task with the specified
        /// <paramref name="operationContext"/>.
        /// </summary>
        /// <param name="operationContext">
        /// The operation context.
        /// </param>
        /// <returns>
        /// The query result that the deferred execution task produced.
        /// </returns>
        Task<IQueryResult> ExecuteAsync(IOperationContext operationContext);

        /// <summary>
        /// Next and previous are properties that are used by the execution engine to
        /// track the execution state.
        /// </summary>
        IDeferredExecutionTask? Next { get; set; }

        /// <summary>
        /// Next and previous are properties that are used by the execution engine to
        /// track the execution state.
        /// </summary>
        IDeferredExecutionTask? Previous { get; set; }
    }
}
