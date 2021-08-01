using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution
{
    /// <summary>
    /// Represents the result of the GraphQL execution pipeline.
    /// </summary>
    /// <remarks>
    /// Execution results are by default disposable and disposing
    /// them allows it to give back its used memory to the execution
    /// engine result pools.
    /// </remarks>
    public interface IExecutionResult : IDisposable
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
