using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution
{
    /// <summary>
    /// Represents the result of the GraphQL execution pipeline.
    /// </summary>
    public interface IExecutionResult
    {
        /// <summary>
        /// Gets the GraphQL errors of the result.
        /// </summary>
        IReadOnlyList<IError>? Errors { get; }

        /// <summary>
        /// Gets the additional information that are passed along
        /// with the result and will be serialized for transport.
        /// </summary>
        IReadOnlyDictionary<string, object?>? Extensions { get; }

        /// <summary>
        /// Gets the result context data which represent additional
        /// properties that are NOT written to the transport.
        /// </summary>
        IReadOnlyDictionary<string, object?>? ContextData { get; }
    }
}
