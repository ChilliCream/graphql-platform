#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Execution.Processing.Pooling;
using HotChocolate.Execution.Properties;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing;

internal sealed partial class ResultBuilder
{
    private readonly ResultPool _resultPool;
    private readonly object _objectSync = new();
    private readonly object _objectListSync = new();
    private readonly object _listSync = new();
    private ResultBucket<ObjectResult>? _objectBucket;
    private ResultBucket<ObjectListResult>? _objectListBucket;
    private ResultBucket<ListResult>? _listBucket;

    public ObjectResult RentObject(int capacity)
    {
        lock (_objectSync)
        {
            if (_objectBucket is null)
            {
                _objectBucket = _resultPool.GetObjectBucket();
                _resultOwner.ObjectBuckets.Add(_objectBucket);
            }

            while (true)
            {
                if (_objectBucket.TryPop(out ObjectResult? obj))
                {
                    obj.EnsureCapacity(capacity);
                    return obj;
                }

                _objectBucket = _resultPool.GetObjectBucket();
                _resultOwner.ObjectBuckets.Add(_objectBucket);
            }
        }
    }

    public ObjectListResult RentObjectList(int capacity)
    {
        lock (_objectListSync)
        {
            if (_objectListBucket is null)
            {
                _objectListBucket = _resultPool.GetObjectListBucket();
                _resultOwner.ObjectListBuckets.Add(_objectListBucket);
            }

            while (true)
            {
                if (_objectListBucket.TryPop(out ObjectListResult? obj))
                {
                    obj.EnsureCapacity(capacity);
                    return obj;
                }

                _objectListBucket = _resultPool.GetObjectListBucket();
                _resultOwner.ObjectListBuckets.Add(_objectListBucket);
            }
        }
    }

    public ListResult RentList(int capacity)
    {
        lock (_listSync)
        {
            if (_listBucket is null)
            {
                _listBucket = _resultPool.GetListBucket();
                _resultOwner.ListBuckets.Add(_listBucket);
            }

            while (true)
            {
                if (_listBucket.TryPop(out ListResult? obj))
                {
                    obj.EnsureCapacity(capacity);
                    return obj;
                }

                _listBucket = _resultPool.GetListBucket();
                _resultOwner.ListBuckets.Add(_listBucket);
            }
        }
    }
}
