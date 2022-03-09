using System;
using System.Collections.Generic;
using System.Text.Json;

namespace HotChocolate.Transport.Sockets.Client;

public sealed class ResultStream
{
    public bool IsSuccessResult => Errors.Count is 0;

    public IReadOnlyList<JsonElement> Errors { get; }

    public IAsyncEnumerable<OperationResult> ReadResultsAsync()
    {
        throw new NotImplementedException();
    }
}
