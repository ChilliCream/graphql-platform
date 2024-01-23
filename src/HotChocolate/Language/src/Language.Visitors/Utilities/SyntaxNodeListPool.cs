using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Language.Visitors;

internal sealed class SyntaxNodeListPool : DefaultObjectPool<List<ISyntaxNode>>
{
    public SyntaxNodeListPool()
        : base(new Policy(), 64)
    {
    }

    private sealed class Policy : IPooledObjectPolicy<List<ISyntaxNode>>
    {
        public List<ISyntaxNode> Create() => [];

        public bool Return(List<ISyntaxNode> obj)
        {
            obj.Clear();
            return true;
        }
    }
}
