using System;
using System.Collections.Generic;

namespace HotChocolate.Execution;

public interface IExecutionRequest
{
    /// <summary>
    /// Gets the initial request state.
    /// </summary>
    IReadOnlyDictionary<string, object?>? ContextData { get; }

    /// <summary>
    /// Gets the services that shall be used while executing the GraphQL request.
    /// </summary>
    IServiceProvider? Services { get; }
}