using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate.ApolloFederation.Extensions;
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
            descriptor.Name(TypeNames.Entity);
        }
    }

    public class EntityTypeInterceptor : TypeInterceptor
    {
        private IList<ObjectType> typesToBeUnioned = new List<ObjectType>();
        public override bool TriggerAggregations { get; } = true;

        public override void OnAfterInitialize(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            AddToUnionIfHasTypeLevelKeyDirective(
                discoveryContext,
                definition
            );

            AggregatePropertyLevelKeyDirectives(
                discoveryContext,
                definition
            );
        }

        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            FillTypeUnionIfEntityType(
                completionContext,
                definition
            );
        }

        private void AddToUnionIfHasTypeLevelKeyDirective(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase definition)
        {
            if (discoveryContext.Type is ObjectType objectType &&
                definition is ObjectTypeDefinition objectTypeDefinition)
            {
                var containsObjectLevelKeyDirective = objectTypeDefinition.Directives.Any(
                    directive => directive.Reference is NameDirectiveReference {Name: {Value: "key"}}
                );

                var containsFieldLevelKeyDirective =
                    objectTypeDefinition.Fields.Any(field => field.ContextData.ContainsKey("key"));

                if (containsObjectLevelKeyDirective || containsFieldLevelKeyDirective)
                {
                    typesToBeUnioned.Add(objectType);
                }
            }
        }

        private void AggregatePropertyLevelKeyDirectives(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase definition)
        {
            if (discoveryContext.Type is ObjectType objectType &&
                definition is ObjectTypeDefinition objectTypeDefinition)
            {
                if (objectTypeDefinition.Fields.Any(
                    field => field.ContextData.ContainsKey(KeyDirectiveType.ContextDataMarkerName)
                ))
                {
                    var strBuilder = new StringBuilder();
                    foreach (ObjectFieldDefinition objectFieldDefinition in objectTypeDefinition.Fields)
                    {
                        if (objectFieldDefinition.ContextData.ContainsKey(
                            KeyDirectiveType.ContextDataMarkerName
                        ))
                        {
                            strBuilder.Append(objectFieldDefinition.Name);
                            strBuilder.Append(" ");
                        }
                    }

                    // Remove last space
                    strBuilder.Length--;

                    objectTypeDefinition.Key(
                        strBuilder.ToString(),
                        discoveryContext.TypeInspector
                    );
                    discoveryContext.RegisterDependencyRange(
                        objectTypeDefinition.Directives.Select(dir => dir.Reference)
                    );
                    discoveryContext.RegisterDependencyRange(
                        objectTypeDefinition.Directives.Select(dir => dir.TypeReference),
                        TypeDependencyKind.Completed
                    );
                    typesToBeUnioned.Add(objectType);
                }
            }
        }

        private void FillTypeUnionIfEntityType(ITypeCompletionContext completionContext, DefinitionBase definition)
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
