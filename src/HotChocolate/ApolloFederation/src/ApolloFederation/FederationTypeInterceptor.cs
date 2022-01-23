using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.ApolloFederation.ThrowHelper;
using static HotChocolate.ApolloFederation.WellKnownContextData;

namespace HotChocolate.ApolloFederation;

internal sealed class FederationTypeInterceptor : TypeInterceptor
{
    private static readonly object _empty = new();
    private static readonly MethodInfo _matches =
        typeof(ReferenceResolverHelper)
            .GetMethod(
                nameof(ReferenceResolverHelper.Matches),
                BindingFlags.Static | BindingFlags.Public)!;
    private static readonly MethodInfo _execute =
        typeof(ReferenceResolverHelper)
            .GetMethod(
                nameof(ReferenceResolverHelper.ExecuteAsync),
                BindingFlags.Static | BindingFlags.Public)!;
    private static readonly MethodInfo _invalid =
        typeof(ReferenceResolverHelper)
            .GetMethod(
                nameof(ReferenceResolverHelper.Invalid),
                BindingFlags.Static | BindingFlags.Public)!;
    private readonly List<ObjectType> _entityTypes = new();

    public override bool TriggerAggregations => true;

    public override void OnAfterInitialize(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
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
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        AddMemberTypesToTheEntityUnionType(
            completionContext,
            definition);

        AddServiceTypeToQueryType(
            completionContext,
            definition);
    }

    public override void OnAfterCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        if (completionContext.Type is ObjectType type &&
            definition is ObjectTypeDefinition typeDef)
        {
            AddFactoryMethodToContext(
                type,
                contextData);

            CompleteReferenceResolver(typeDef);
        }
    }

    private void AddFactoryMethodToContext(
        ObjectType type,
        IDictionary<string, object?> contextData)
    {
        if (type.ToRuntimeType() is var rt &&
            rt.IsDefined(typeof(ForeignServiceTypeExtensionAttribute)))
        {
            IEnumerable<ObjectField> fields = type.Fields.Where(
                field => field.Member is not null &&
                    field.Member.IsDefined(typeof(ExternalAttribute)));

            ParameterExpression representationArgument =
                Expression.Parameter(typeof(Representation));
            ParameterExpression objectVariable =
                Expression.Variable(rt);

            IEnumerable<ConditionalExpression> assignExpressions = fields.Select(
                field =>
                {
                    if (field.Member is PropertyInfo pi && field.Type.InnerType() is ScalarType st)
                    {
                        Expression<Func<IValueNode, object?>> valueConverter =
                            valueNode => st.ParseLiteral(valueNode);

                        Expression<Func<Representation, bool>> assignConditionCheck =
                            representation =>
                                representation.Data.Fields.Any(
                                    item =>
                                        item.Name.Value.Equals(
                                            pi.Name,
                                            StringComparison.OrdinalIgnoreCase));

                        Expression<Func<Representation, IValueNode>> valueGetterFunc =
                            representation =>
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
                                            representationArgument)),
                                    pi.PropertyType)));
                    }

                    throw ExternalAttribute_InvalidTarget(rt, field.Member);
                });

            LabelTarget returnTarget = Expression.Label(rt);
            var expressions = new List<Expression>
                {
                    Expression.Assign(objectVariable, Expression.New(rt))
                };
            expressions.AddRange(assignExpressions);
            expressions.Add(Expression.Label(returnTarget, objectVariable));

            LambdaExpression objectFactoryMethodExpression = Expression.Lambda(
                Expression.Block(new[] { objectVariable }, expressions),
                representationArgument);

            contextData[EntityResolver] = objectFactoryMethodExpression.Compile();
        }
    }

    private void CompleteReferenceResolver(ObjectTypeDefinition typeDef)
    {
        if (typeDef.GetContextData().TryGetValue(EntityResolver, out var value) &&
            value is IReadOnlyList<ReferenceResolverDefinition> resolvers)
        {
            if (resolvers.Count == 1)
            {
                typeDef.ContextData[EntityResolver] = resolvers[0].Resolver;
            }
            else
            {
                var expressions = new Stack<(Expression Condition, Expression Execute)>();
                ParameterExpression context = Expression.Parameter(typeof(IResolverContext));

                foreach (ReferenceResolverDefinition resolverDef in resolvers)
                {
                    Expression required = Expression.Constant(resolverDef.Required);
                    Expression resolver = Expression.Constant(resolverDef.Resolver);
                    Expression condition = Expression.Call(_matches, context, required);
                    Expression execute = Expression.Call(_execute, context, resolver);
                    expressions.Push((condition, execute));
                }

                Expression current = Expression.Call(_invalid, context);
                ParameterExpression variable = Expression.Variable(typeof(ValueTask<object?>));

                while (expressions.Count > 0)
                {
                    var expression = expressions.Pop();
                    current = Expression.IfThenElse(
                        expression.Condition,
                        Expression.Assign(variable, expression.Execute),
                        current);
                }

                current = Expression.Block(new[] { variable }, current, variable);

                typeDef.ContextData[EntityResolver] =
                    Expression.Lambda<FieldResolverDelegate>(current, context).Compile();
            }
        }
    }

    private void AddServiceTypeToQueryType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition)
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
                    c.ArgumentValue<IReadOnlyList<Representation>>(
                        WellKnownArgumentNames.Representations),
                    c
                ));
            objectTypeDefinition.Fields.Add(entitiesFieldDescriptor.CreateDefinition());
        }
    }

    private void AddToUnionIfHasTypeLevelKeyDirective(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition)
    {
        if (discoveryContext.Type is ObjectType objectType &&
            definition is ObjectTypeDefinition objectTypeDefinition)
        {
            if (objectTypeDefinition.Directives.Any(
                    d => d.Reference is NameDirectiveReference
                    { Name: { Value: WellKnownTypeNames.Key } }) ||
                objectTypeDefinition.Fields.Any(
                    f => f.ContextData.ContainsKey(WellKnownTypeNames.Key)))
            {
                _entityTypes.Add(objectType);
            }
        }
    }

    private void AggregatePropertyLevelKeyDirectives(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition)
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

                foreach (ObjectFieldDefinition? fieldDefinition in fields)
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
                foreach (DirectiveDefinition directiveDefinition in objectTypeDefinition.Directives)
                {
                    discoveryContext.Dependencies.Add(
                        new TypeDependency(
                            directiveDefinition.TypeReference,
                            TypeDependencyKind.Completed));

                    discoveryContext.RegisterDependency(directiveDefinition.Reference);
                }

                // since this type has now a key directive we also need to add this type to
                // the _Entity union type.
                _entityTypes.Add(objectType);
            }
        }
    }

    private void AddMemberTypesToTheEntityUnionType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition)
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
