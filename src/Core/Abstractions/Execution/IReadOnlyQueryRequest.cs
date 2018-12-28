using System;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public interface IReadOnlyQueryRequest
    {
        string Query { get; }

        string OperationName { get; }

        IReadOnlyDictionary<string, object> VariableValues { get; }

        object InitialValue { get; }

        IReadOnlyDictionary<string, object> Properties { get; }

        IServiceProvider Services { get; }
    }
}
