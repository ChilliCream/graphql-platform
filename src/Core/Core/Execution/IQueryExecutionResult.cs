using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public interface IQueryExecutionResult
        : IExecutionResult
    {
        IOrderedDictionary Data { get; }

        T ToObject<T>();

        string ToJson();
    }
}
