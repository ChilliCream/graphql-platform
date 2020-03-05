using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution
{
    public interface IReadOnlyQueryResult
        : IExecutionResult
        , IDisposable
    {
        IReadOnlyDictionary<string, object?>? Data { get; }

        IReadOnlyDictionary<string, object?> ToDictionary();
    }
}
