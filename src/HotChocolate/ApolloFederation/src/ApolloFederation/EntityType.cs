using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate.ApolloFederation.Extensions;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Configuration;
using HotChocolate.Language;
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

        public override void OnAfterInitialize(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            // Add types to union if they contain a key directive
            if (definition is ObjectTypeDefinition objectTypeDefinition &&
                discoveryContext.Type is ObjectType objectType)
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

        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            AggregatePropertyLevelKeyDefinitions(
                completionContext,
                definition
            );

            FillTypeUnionIfEntityType(
                completionContext,
                definition
            );
        }

        private void AggregatePropertyLevelKeyDefinitions(
            ITypeCompletionContext completionContext,
            DefinitionBase definition)
        {
            if (completionContext.Type is ObjectType && definition is ObjectTypeDefinition objectTypeDefinition)
            {
                if (objectTypeDefinition.Fields.Any(
                    field => field.ContextData.ContainsKey(FederationResources.KeyDirective_ContextDataMarkerName)
                ))
                {
                    var strBuilder = new StringBuilder();
                    foreach (ObjectFieldDefinition objectFieldDefinition in objectTypeDefinition.Fields)
                    {
                        if (objectFieldDefinition.ContextData.ContainsKey(
                            FederationResources.KeyDirective_ContextDataMarkerName
                        ))
                        {
                            strBuilder.Append(objectFieldDefinition.Name);
                            strBuilder.Append(" ");
                        }
                    }

                    objectTypeDefinition.Key(strBuilder.ToString());
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
