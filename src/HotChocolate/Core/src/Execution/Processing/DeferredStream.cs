using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing.Tasks;

namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// Represents the work to executed the deferred elements of a stream.
    /// </summary>
    internal sealed class DeferredStream : IDeferredExecutionTask
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DeferredFragment"/>.
        /// </summary>
        public DeferredStream(
            ISelection selection,
            string? label,
            Path path,
            object? parent,
            int index,
            IAsyncEnumerator<object?> enumerator,
            IImmutableDictionary<string, object?> scopedContextData)
        {
            Selection = selection;
            Label = label;
            Path = path;
            Parent = parent;
            Index = index;
            Enumerator = enumerator;
            ScopedContextData = scopedContextData;
        }

        /// <summary>
        /// Gets the selection of the streamed field.
        /// </summary>
        public ISelection Selection { get; }

        /// <summary>
        /// If this argument label has a value other than null, it will be passed
        /// on to the result of this defer directive. This label is intended to
        /// give client applications a way to identify to which fragment a deferred
        /// result belongs to.
        /// </summary>
        public string? Label { get; }

        /// <summary>
        /// Gets the result path into which this deferred fragment shall be patched.
        /// </summary>
        public Path Path { get; }

        /// <summary>
        /// Gets the index of the last element.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// Gets the parent / source value.
        /// </summary>
        public object? Parent { get; }

        /// <summary>
        /// Gets the enumerator to retrieve the payloads of the stream.
        /// </summary>
        public IAsyncEnumerator<object?> Enumerator { get; }

        /// <summary>
        /// Gets the preserved scoped context from the parent resolver.
        /// </summary>
        public IImmutableDictionary<string, object?> ScopedContextData { get; }

        /// <inheritdoc/>
        public IDeferredExecutionTask? Next { get; set; }

        /// <inheritdoc/>
        public IDeferredExecutionTask? Previous { get; set; }

        /// <inheritdoc/>
        public async Task<IQueryResult?> ExecuteAsync(IOperationContext operationContext)
        {
            operationContext.QueryPlan = operationContext.QueryPlan.GetStreamPlan(Selection.Id);

            Index++;
            var hasNext = await Enumerator.MoveNextAsync();

            if (hasNext)
            {
                ResolverTask resolverTask = ResolverTaskFactory.EnqueueElementTasks(
                    operationContext,
                    Selection,
                    Parent,
                    Path,
                    Index,
                    Enumerator,
                    ScopedContextData);

                if (!operationContext.Scheduler.IsEmpty)
                {
                    await operationContext.Scheduler.ExecuteAsync().ConfigureAwait(false);
                }

                operationContext.Scheduler.DeferredWork.Register(this);

                IQueryResult result = operationContext
                    .TrySetNext(true)
                    .SetLabel(Label)
                    .SetPath(Path.Append(Index))
                    .SetData((ResultMap)resolverTask.ResultMap[0].Value!)
                    .BuildResult();

                resolverTask.CompleteUnsafe();

                return result;
            }

            return null;
        }
    }
}
