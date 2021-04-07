using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace StrawberryShake.VisualStudio.Language
{
    internal sealed class SyntaxNodeListPool
        : DefaultObjectPool<List<ISyntaxNode>>
    {
        public SyntaxNodeListPool()
            : base(new Policy(), 64)
        {
        }

        private class Policy : IPooledObjectPolicy<List<ISyntaxNode>>
        {
            public List<ISyntaxNode> Create() => new List<ISyntaxNode>();

            public bool Return(List<ISyntaxNode> obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
