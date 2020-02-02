using System;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public interface IReadOnlyQueryRequest
    {
        IQuery Query { get; }

        string QueryName { get; }

        string QueryHash { get; }

        string OperationName { get; }

        IReadOnlyDictionary<string, object> VariableValues { get; }

        object InitialValue { get; }

        IReadOnlyDictionary<string, object> Properties { get; }

        IReadOnlyDictionary<string, object> Extensions { get; }

        IServiceProvider Services { get; }
    }
}
