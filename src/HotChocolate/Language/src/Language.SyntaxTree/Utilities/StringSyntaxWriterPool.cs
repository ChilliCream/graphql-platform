using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Language.Utilities;

internal sealed class StringSyntaxWriterPool() : DefaultObjectPool<StringSyntaxWriter>(new Policy(), 8)
{
    private sealed class Policy : IPooledObjectPolicy<StringSyntaxWriter>
    {
        public StringSyntaxWriter Create() => new();

        public bool Return(StringSyntaxWriter obj)
        {
            obj.Clear();
            return true;
        }
    }
}
