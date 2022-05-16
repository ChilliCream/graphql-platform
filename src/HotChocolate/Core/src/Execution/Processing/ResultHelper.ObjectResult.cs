#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Execution.Processing.Pooling;
using HotChocolate.Execution.Properties;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing;

internal sealed partial class ResultHelper
{
    private ResultPool _pool;

    private readonly object _objectSync = new();
    private ResultBucket<ObjectResult>? _objectBuffer;

    public ObjectResult RentObject()
    {
        lock (_objectSync)
        {
            if (_objectBuffer is null)
            {
                _objectBuffer = _pool.GetObjectBuffer();
                _resultOwner.ObjectBuffers.Add(_objectBuffer);
            }

            while (true)
            {
                if (_objectBuffer.TryPop(out ObjectResult? obj))
                {
                    return obj;
                }

                _objectBuffer = _pool.GetObjectBuffer();
                _resultOwner.ObjectBuffers.Add(_objectBuffer);
            }
        }
    }
}
