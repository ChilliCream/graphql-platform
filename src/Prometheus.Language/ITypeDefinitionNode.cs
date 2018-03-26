using System.Collections.Generic;

namespace Prometheus.Language
{
    public interface ITypeDefinitionNode
        : ITypeSystemDefinitionNode
    {
        NameNode Name { get; }
        StringValueNode Description { get; }
        IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}