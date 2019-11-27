using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IResponseStream
        : IAsyncEnumerable<IOperationResult>
        , IAsyncDisposable
    {

    }
}
