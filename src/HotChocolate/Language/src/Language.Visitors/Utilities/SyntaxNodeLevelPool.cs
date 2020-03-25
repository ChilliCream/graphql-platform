using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Language.Visitors
{
    internal sealed class SyntaxNodeLevelPool
        : DefaultObjectPool<List<List<ISyntaxNode>>>
    {
        public SyntaxNodeLevelPool()
            : base(new Policy(), 8)
        {
        }

        private class Policy : IPooledObjectPolicy<List<List<ISyntaxNode>>>
        {
            public List<List<ISyntaxNode>> Create() => new List<List<ISyntaxNode>>();

            public bool Return(List<List<ISyntaxNode>> obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
