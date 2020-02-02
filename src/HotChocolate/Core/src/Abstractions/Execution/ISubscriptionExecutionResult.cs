using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public interface ISubscriptionExecutionResult
        : IExecutionResult
        , IResponseStream
    {
        new IDictionary<string, object> ContextData { get; }
    }
}
