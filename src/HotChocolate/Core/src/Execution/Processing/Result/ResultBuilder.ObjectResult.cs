#nullable enable

namespace HotChocolate.Execution.Processing;

internal sealed partial class ResultBuilder
{
    private readonly ResultPool _resultPool;
    private readonly object _objectSync = new();
    private readonly object _listSync = new();
    private ResultBucket<ObjectResult>? _objectBucket;
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
                if (_objectBucket.TryPop(out var obj))
                {
                    obj.EnsureCapacity(capacity);
                    return obj;
                }

                _objectBucket = _resultPool.GetObjectBucket();
                _resultOwner.ObjectBuckets.Add(_objectBucket);
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
                if (_listBucket.TryPop(out var obj))
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
