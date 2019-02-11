using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public interface ISchemaInfo
    {
        string Name { get; }

        DocumentNode Document { get; }

        IReadOnlyDictionary<string, ITypeDefinitionNode> Types { get; }

        IReadOnlyDictionary<string, DirectiveDefinitionNode> Directives { get; }

        ObjectTypeDefinitionNode QueryType { get; }

        ObjectTypeDefinitionNode MutationType { get; }

        ObjectTypeDefinitionNode SubscriptionType { get; }

        bool IsRootType(ITypeDefinitionNode typeDefinition);
    }
}
