using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Validation
{
    internal sealed class FieldInfoListBufferPool
        : DefaultObjectPool<FieldInfoListBuffer>
    {
        public FieldInfoListBufferPool(int size = 16)
            : base(new Policy(), size)
        {
        }

        private class Policy : IPooledObjectPolicy<FieldInfoListBuffer>
        {
            public FieldInfoListBuffer Create() => new FieldInfoListBuffer();

            public bool Return(FieldInfoListBuffer obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
