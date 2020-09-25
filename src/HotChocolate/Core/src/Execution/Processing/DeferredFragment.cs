using System.Collections.Immutable;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// Represents a deprioritized fragment of the query that will be executed after
    /// the main execution has finished.
    /// </summary>
    internal sealed class DeferredFragment : IDeferredExecutionTask
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DeferredFragment"/>.
        /// </summary>
        public DeferredFragment(
            IFragment fragment,
            string? label,
            Path path,
            object? value,
            IImmutableDictionary<string, object?> scopedContextData)
        {
            Fragment = fragment;
            Label = label;
            Path = path;
            Value = value;
            ScopedContextData = scopedContextData;
        }

        /// <summary>
        /// Gets the deferred fragment.
        /// </summary>
        public IFragment Fragment { get; }

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
        /// Gets the parent / source value.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Gets the preserved scoped context from the parent resolver.
        /// </summary>
        public IImmutableDictionary<string, object?> ScopedContextData { get; }

        /// <inheritdoc/>
        public async Task<IQueryResult> ExecuteAsync(IOperationContext operationContext)
        {
            ResultMap resultMap = Fragment.SelectionSet.EnqueueResolverTasks(
                operationContext,
                Path,
                ScopedContextData,
                Value);

            await ResolverExecutionHelper
                .ExecuteTasksAsync(operationContext)
                .ConfigureAwait(false);

            return operationContext
                .TrySetNext(true)
                .SetLabel(Label)
                .SetPath(Path)
                .SetData(resultMap)
                .BuildResult();
        }
    }
}
