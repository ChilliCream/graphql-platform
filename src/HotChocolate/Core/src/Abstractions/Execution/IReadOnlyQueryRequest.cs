﻿using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution
{
    public interface IReadOnlyQueryRequest
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
    }
}
