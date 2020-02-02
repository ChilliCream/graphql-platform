using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IResponseStream<T>
        : IAsyncEnumerable<IOperationResult<T>>
        , IAsyncDisposable
        where T : class
    {

    }
}
