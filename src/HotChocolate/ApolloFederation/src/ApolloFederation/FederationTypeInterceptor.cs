using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
    internal class FederationTypeInterceptor : TypeInterceptor
    {
        private readonly List<ObjectType> _entityTypes = new List<ObjectType>();
        private static readonly object _empty = new object();

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

        public override void OnAfterCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            AddFactoryMethodToContext(
                completionContext,
                contextData);
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

        private void AddFactoryMethodToContext(
            ITypeCompletionContext completionContext,
            IDictionary<string, object?> contextData)
        {
            if (completionContext.Type is ObjectType ot && ot.ToRuntimeType() is var rt &&
                rt.IsDefined(typeof(ForeignServiceTypeExtensionAttribute)))
            {
                var fields = ot.Fields.Where(
                    field => field.Member is not null &&
                             field.Member.IsDefined(typeof(ExternalAttribute))
                );

                var representationArgument = Expression.Parameter(typeof(Representation));
                var objectVariable = Expression.Variable(rt);

                var assignExpressions = fields.Select(
                    field =>
                    {
                        if (field.Member is PropertyInfo pi && field.Type.InnerType() is ScalarType st)
                        {
                            Expression<Func<IValueNode, object?>> valueConverter =
                                    (valueNode) => st.ParseLiteral(valueNode, true);

                            Expression<Func<Representation, bool>> assignConditionCheck =
                                (representation) =>
                                    representation.Data.Fields.Any(
                                        item =>
                                            item.Name.Value.Equals(
                                                pi.Name,
                                                StringComparison.OrdinalIgnoreCase)
                                    );

                            Expression<Func<Representation, IValueNode>> valueGetterFunc =
                                (representation) =>
                                    representation.Data.Fields.Single(item =>
                                        item.Name.Value.Equals(
                                            pi.Name,
                                            StringComparison.OrdinalIgnoreCase)).Value;

                            return Expression.IfThen(
                                Expression.Invoke(assignConditionCheck, representationArgument),
                                Expression.Assign(
                                    Expression.MakeMemberAccess(objectVariable, pi),
                                    Expression.Convert(
                                        Expression.Invoke(
                                            valueConverter,
                                            Expression.Invoke(
                                                valueGetterFunc,
                                                representationArgument
                                            )), pi.PropertyType)));
                        }
                        throw ExternalAttribute_InvalidTarget(rt, field.Member);
                    }
                );

                LabelTarget returnTarget = Expression.Label(rt);
                var expressions = new List<Expression>
                {
                    Expression.Assign(
                        objectVariable,
                        Expression.New(rt)
                    ),
                };
                expressions.AddRange(assignExpressions);
                expressions.Add(Expression.Label(returnTarget, objectVariable));

                var objectFactoryMethodExpression = Expression.Lambda(
                    Expression.Block(
                        new[] { objectVariable },
                        expressions
                    ),
                    representationArgument
                );

                contextData[EntityResolver] = objectFactoryMethodExpression.Compile();
            }
        }

        private void AddServiceTypeToQueryType(
            ITypeCompletionContext completionContext,
            DefinitionBase definition)
        {
            if (completionContext.IsQueryType == true &&
                definition is ObjectTypeDefinition objectTypeDefinition)
            {
                var serviceFieldDescriptor = ObjectFieldDescriptor.New(
                    completionContext.DescriptorContext,
                    WellKnownFieldNames.Service);
                serviceFieldDescriptor
                    .Type<NonNullType<ServiceType>>()
                    .Resolve(_empty);
                objectTypeDefinition.Fields.Add(serviceFieldDescriptor.CreateDefinition());

                var entitiesFieldDescriptor = ObjectFieldDescriptor.New(
                    completionContext.DescriptorContext,
                    WellKnownFieldNames.Entities);
                entitiesFieldDescriptor
                    .Type<NonNullType<ListType<EntityType>>>()
                    .Argument(
                        WellKnownArgumentNames.Representations,
                        descriptor =>
                            descriptor.Type<NonNullType<ListType<NonNullType<AnyType>>>>()
                    )
                    .Resolve(c => EntitiesResolver._Entities(
                        c.Schema,
                        c.ArgumentValue<IReadOnlyList<Representation>>(WellKnownArgumentNames.Representations),
                        c
                    ));
                objectTypeDefinition.Fields.Add(entitiesFieldDescriptor.CreateDefinition());
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
