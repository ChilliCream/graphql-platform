using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IResponseStream<T>
        : IResponseStream
        , IAsyncEnumerable<IOperationResult<T>>
        where T : class
    {

    }
}
