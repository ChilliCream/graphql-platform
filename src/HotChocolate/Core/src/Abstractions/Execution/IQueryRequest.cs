using System;
using System.Collections.Generic;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Execution
{
    public interface IQueryRequest
    {
        IQuery? Query { get; }

        string? QueryId { get; }

        string? QueryHash { get; }

        string? OperationName { get; }

        IReadOnlyDictionary<string, object?>? VariableValues { get; }

        object? InitialValue { get; }

        IReadOnlyDictionary<string, object?>? ContextData { get; }

        IReadOnlyDictionary<string, object?>? Extensions { get; }

        IServiceProvider? Services { get; }

        OperationType[]? AllowedOperations { get; }
    }
}
