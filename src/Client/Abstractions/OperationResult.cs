using System.Collections.Generic;

namespace StrawberryShake
{
    internal sealed class OperationResult<T>
        : IOperationResult<T>
    {
        public T Data { get; set; }

        public IReadOnlyList<IError> Errors { get; set; }

        public IReadOnlyDictionary<string, object> Extensions { get; set; }

        object IOperationResult.Data => Data;

        public void EnsureNoErrors()
        {
            if (Errors.Count > 0)
            {
                throw new GraphQLException(Errors);
            }
        }
    }
}
