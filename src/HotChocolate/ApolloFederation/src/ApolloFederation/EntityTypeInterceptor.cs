using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.ApolloFederation.WellKnownContextData;
using static HotChocolate.ApolloFederation.ThrowHelper;

namespace HotChocolate.ApolloFederation
{
    internal class EntityTypeInterceptor : TypeInterceptor
    {
        private readonly List<ObjectType> _entityTypes = new List<ObjectType>();

        public override bool TriggerAggregations { get; } = true;

        public override void OnAfterInitialize(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            AddToUnionIfHasTypeLevelKeyDirective(
                discoveryContext,
                definition);

            AggregatePropertyLevelKeyDirectives(
                discoveryContext,
                definition);
        }

        public override void OnTypesInitialized(
            IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
        {
            if (_entityTypes.Count == 0)
            {
                throw EntityType_NoEntities();
            }
        }

        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            AddMemberTypesToTheEntityUnionType(
                completionContext,
                definition);

            AddServiceTypeToQueryType(
                completionContext,
                definition);
        }

        private void AddServiceTypeToQueryType(
            ITypeCompletionContext completionContext,
            DefinitionBase definition)
        {
            if (completionContext.IsQueryType == true &&
                definition is ObjectTypeDefinition objectTypeDefinition)
            {
                var fieldDescriptor = ObjectFieldDescriptor.New(
                    completionContext.DescriptorContext,
                    WellKnownFieldNames.Service);
                fieldDescriptor
                    .Type<NonNullType<ServiceType>>()
                    .Resolver(default(object));
                objectTypeDefinition.Fields.Add(fieldDescriptor.CreateDefinition());
            }
        }

        private void AddToUnionIfHasTypeLevelKeyDirective(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase definition)
        {
            if (discoveryContext.Type is ObjectType objectType &&
                definition is ObjectTypeDefinition objectTypeDefinition)
            {
                if (objectTypeDefinition.Directives.Any(
                        d => d.Reference is NameDirectiveReference
                            { Name: { Value: WellKnownTypeNames.Key }}) ||
                    objectTypeDefinition.Fields.Any(
                        f => f.ContextData.ContainsKey(WellKnownTypeNames.Key)))
                {
                    _entityTypes.Add(objectType);
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
                // if we find key markers on our fields, we need to construct the key directive
                // from the annotated fields.
                if (objectTypeDefinition.Fields.Any(f => f.ContextData.ContainsKey(KeyMarker)))
                {
                    IReadOnlyList<ObjectFieldDefinition> fields = objectTypeDefinition.Fields;
                    var fieldSet = new StringBuilder();

                    foreach (var fieldDefinition in fields)
                    {
                        if (fieldDefinition.ContextData.ContainsKey(KeyMarker))
                        {
                            if (fieldSet.Length > 0)
                            {
                                fieldSet.Append(' ');
                            }

                            fieldSet.Append(fieldDefinition.Name);
                        }
                    }

                    // add the key directive with the dynamically generated field set.
                    AddKeyDirective(objectTypeDefinition, fieldSet.ToString());

                    // register dependency to the key directive so that it is completed before
                    // we complete this type.
                    discoveryContext.RegisterDependencyRange(
                        objectTypeDefinition.Directives.Select(dir => dir.Reference));
                    discoveryContext.RegisterDependencyRange(
                        objectTypeDefinition.Directives.Select(dir => dir.TypeReference),
                        TypeDependencyKind.Completed);

                    // since this type has now a key directive we also need to add this type to
                    // the _Entity union type.
                    _entityTypes.Add(objectType);
                }
            }
        }

        private void AddMemberTypesToTheEntityUnionType(
            ITypeCompletionContext completionContext,
            DefinitionBase definition)
        {
            if (completionContext.Type is EntityType &&
                definition is UnionTypeDefinition unionTypeDefinition)
            {
                foreach (ObjectType objectType in _entityTypes)
                {
                    unionTypeDefinition.Types.Add(TypeReference.Create(objectType));
                }
            }
        }

        private static void AddKeyDirective(
            ObjectTypeDefinition objectTypeDefinition,
            string fieldSet)
        {
            var directiveNode = new DirectiveNode(
                WellKnownTypeNames.Key,
                new ArgumentNode(
                    WellKnownArgumentNames.Fields,
                    fieldSet));

            objectTypeDefinition.Directives.Add(
                new DirectiveDefinition(directiveNode));
        }
    }
}
