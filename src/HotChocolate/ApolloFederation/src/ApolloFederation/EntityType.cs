using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.ApolloFederation
{
    public class EntityType : UnionType
    {
        protected override void Configure(IUnionTypeDescriptor descriptor)
        {
            descriptor.Name("_Entity");
        }
    }

    public class EntityTypeInterceptor : TypeInterceptor
    {
        private IList<ObjectType> typesToBeUnioned = new List<ObjectType>();
        public override bool TriggerAggregations { get; } = true;

        public override void OnAfterInitialize(ITypeDiscoveryContext discoveryContext, DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            if (definition is ObjectTypeDefinition objectTypeDefinition &&
                discoveryContext.Type is ObjectType objectType)
            {
                if (objectTypeDefinition.Directives.Any(
                    directive => directive.Reference is NameDirectiveReference {Name: {Value: "key"}}))
                {
                    typesToBeUnioned.Add(objectType);
                }
            }
        }

        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            if (completionContext.Type is EntityType entityType &&
                definition is UnionTypeDefinition unionTypeDefinition)
            {
                foreach (ObjectType objectType in typesToBeUnioned)
                {
                    unionTypeDefinition.Types.Add(TypeReference.Create(objectType));
                }
            }
        }
    }
}
