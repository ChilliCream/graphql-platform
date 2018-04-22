using System.Collections.Generic;

namespace HotChocolate.Types
{
    public interface ITypeSystemNode
        : IHasSyntaxNode
    {
        IEnumerable<ITypeSystemNode> GetNodes();
    }
}