using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Stitching.Execution;

internal sealed class SelectionListObjectPool : ObjectPool<List<ISelection>>
{
    private const int _maxSize = 16;
    private List<List<ISelection>> _pool = new();

    public override List<ISelection> Get()
    {
        return _pool.TryPop(out var selection)
            ? selection
            : new List<ISelection>();
    }

    public override void Return(List<ISelection> obj)
    {
        obj.Clear();

        if (_pool.Count < _maxSize)
        {
            _pool.Push(obj);
        }
    }
}
