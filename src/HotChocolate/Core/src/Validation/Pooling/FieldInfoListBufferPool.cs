using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Validation;

internal sealed class FieldInfoListBufferPool(int size = 16)
    : DefaultObjectPool<FieldInfoListBuffer>(new Policy(), size)
{
    private sealed class Policy : IPooledObjectPolicy<FieldInfoListBuffer>
    {
        public FieldInfoListBuffer Create() => new();

        public bool Return(FieldInfoListBuffer obj)
        {
            obj.Clear();
            return true;
        }
    }
}
