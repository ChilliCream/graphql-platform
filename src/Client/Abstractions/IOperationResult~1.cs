using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IOperationResult<out T>
        : IOperationResult
    {
        new T Data { get; }
    }

    public class OperationResult<T>
        : IOperationResult<T>
    {
        public T Data { get; }

        public IReadOnlyList<IError> Errors => throw new System.NotImplementedException();

        public IReadOnlyDictionary<string, object> Extensions => throw new System.NotImplementedException();

        object IOperationResult.Data => throw new System.NotImplementedException();

        public void EnsureNoErrors()
        {
            throw new System.NotImplementedException();
        }
    }
}
