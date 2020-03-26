using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Language.Utilities
{
    internal sealed class StringSyntaxWriterPool
        : DefaultObjectPool<StringSyntaxWriter>
    {
        public StringSyntaxWriterPool()
            : base(new Policy(), 8)
        {
        }

        private class Policy : IPooledObjectPolicy<StringSyntaxWriter>
        {
            public StringSyntaxWriter Create() => new StringSyntaxWriter();

            public bool Return(StringSyntaxWriter obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
