using System;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public interface IExecutionResult
    {
        IReadOnlyCollection<IQueryError> Errors { get; }
    }

    public interface IQueryExecutionResult
        : IExecutionResult
    {
        IOrderedDictionary Data { get; }

        T ToObject<T>();

        string ToJson();
    }

    public interface IOrderedDictionary
        : IReadOnlyDictionary<string, object>
    {

    }

    public interface ISubscriptionExecutionResult
        : IExecutionResult
        , IObservable<IQueryExecutionResult>
    {

    }
}
