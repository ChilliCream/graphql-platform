using System.Collections.Generic;

namespace HotChocolate.Language
{
    public interface ITypeDefinitionNode
        : ITypeSystemDefinitionNode
    {
        NameNode Name { get; }
        StringValueNode Description { get; }
        IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}