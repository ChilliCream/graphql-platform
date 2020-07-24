using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public interface ISubscriptionResult
        : IExecutionResult
        , IResponseStream
    {
    }
}
