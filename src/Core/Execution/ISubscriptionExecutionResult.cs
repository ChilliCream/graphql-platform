using System;

namespace HotChocolate.Execution
{
    public interface ISubscriptionExecutionResult
        : IExecutionResult
        , IObservable<IQueryExecutionResult>
    {
    }
}
