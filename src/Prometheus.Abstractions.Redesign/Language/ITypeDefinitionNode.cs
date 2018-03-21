using System.Collections.Generic;

namespace Prometheus.Language
{
    // Type Definition

    public interface ITypeDefinitionNode
      : ITypeSystemDefinitionNode
    {
        NameNode Name { get; }
        StringValueNode Description { get; }
        IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}