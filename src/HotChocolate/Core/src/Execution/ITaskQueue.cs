using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    internal interface ITaskQueue
    {
        /// <summary>
        /// 
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// 
        /// </summary>
        bool TryDequeue([NotNullWhen(true)] out ResolverTask? task);

        /// <summary>
        /// 
        /// </summary>
        void Enqueue(
            IPreparedSelection selection,
            int responseIndex,
            ResultMap resultMap,
            object? parent,
            Path path,
            IImmutableDictionary<string, object?> scopedContextData);
    }
}
