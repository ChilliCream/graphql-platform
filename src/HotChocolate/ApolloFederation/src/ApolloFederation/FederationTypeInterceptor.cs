using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Descriptors;
using HotChocolate.ApolloFederation.Helpers;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.ApolloFederation.ThrowHelper;
using static HotChocolate.ApolloFederation.Constants.WellKnownContextData;

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

    public override void OnAfterInitialize(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition)
    {
        if (discoveryContext.Type is ObjectType objectType &&
            definition is ObjectTypeDefinition objectTypeDefinition)
        {
            ApplyMethodLevelReferenceResolvers(
                objectType,
                objectTypeDefinition,
                discoveryContext);

            AddToUnionIfHasTypeLevelKeyDirective(
                objectType,
                objectTypeDefinition);

            AggregatePropertyLevelKeyDirectives(
                objectType,
                objectTypeDefinition,
                discoveryContext);
        }
    }

    public override void OnTypesInitialized()
    {
        if (_entityTypes.Count == 0)
        {
            throw EntityType_NoEntities();
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
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
        DefinitionBase definition)
    {
        if (completionContext.Type is ObjectType type &&
            definition is ObjectTypeDefinition typeDef)
        {
            CompleteExternalFieldSetters(type, typeDef);
            CompleteReferenceResolver(typeDef);
        }
    }

    private void CompleteExternalFieldSetters(ObjectType type, ObjectTypeDefinition typeDef)
        => ExternalSetterExpressionHelper.TryAddExternalSetter(type, typeDef);

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
                var context = Expression.Parameter(typeof(IResolverContext));

                foreach (var resolverDef in resolvers)
                {
                    Expression required = Expression.Constant(resolverDef.Required);
                    Expression resolver = Expression.Constant(resolverDef.Resolver);
                    Expression condition = Expression.Call(_matches, context, required);
                    Expression execute = Expression.Call(_execute, context, resolver);
                    expressions.Push((condition, execute));
                }

                Expression current = Expression.Call(_invalid, context);
                var variable = Expression.Variable(typeof(ValueTask<object?>));

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
                    descriptor => descriptor.Type<NonNullType<ListType<NonNullType<AnyType>>>>())
                .Resolve(c => EntitiesResolver.ResolveAsync(
                    c.Schema,
                    c.ArgumentValue<IReadOnlyList<Representation>>(
                        WellKnownArgumentNames.Representations),
                    c
                ));
            objectTypeDefinition.Fields.Add(entitiesFieldDescriptor.CreateDefinition());
        }
    }

    private void ApplyMethodLevelReferenceResolvers(
        ObjectType objectType,
        ObjectTypeDefinition objectTypeDefinition,
        ITypeDiscoveryContext discoveryContext)
    {
        if (objectType.RuntimeType != typeof(object))
        {
            var descriptorContext = discoveryContext.DescriptorContext;
            var typeInspector = discoveryContext.TypeInspector;
            var descriptor = ObjectTypeDescriptor.From(descriptorContext, objectTypeDefinition);

            foreach (var possibleReferenceResolver in
                objectType.RuntimeType.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                if (possibleReferenceResolver.IsDefined(typeof(ReferenceResolverAttribute)))
                {
                    typeInspector.ApplyAttributes(
                        descriptorContext,
                        descriptor,
                        possibleReferenceResolver);
                }
            }

            descriptor.CreateDefinition();
        }
    }

    private void AddToUnionIfHasTypeLevelKeyDirective(
        ObjectType objectType,
        ObjectTypeDefinition objectTypeDefinition)
    {
        if (objectTypeDefinition.Directives.Any(
            d => d.Value is DirectiveNode { Name.Value: WellKnownTypeNames.Key }) ||
            objectTypeDefinition.Fields.Any(f => f.ContextData.ContainsKey(WellKnownTypeNames.Key)))
        {
            _entityTypes.Add(objectType);
        }
    }

    private void AggregatePropertyLevelKeyDirectives(
        ObjectType objectType,
        ObjectTypeDefinition objectTypeDefinition,
        ITypeDiscoveryContext discoveryContext)
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
            foreach (var directiveDefinition in objectTypeDefinition.Directives)
            {
                discoveryContext.Dependencies.Add(
                    new TypeDependency(
                        directiveDefinition.Type,
                        TypeDependencyFulfilled.Completed));

                discoveryContext.Dependencies.Add(new(directiveDefinition.Type));
            }

            // since this type has now a key directive we also need to add this type to
            // the _Entity union type.
            _entityTypes.Add(objectType);
        }
    }

    private void AddMemberTypesToTheEntityUnionType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition)
    {
        if (completionContext.Type is EntityType &&
            definition is UnionTypeDefinition unionTypeDefinition)
        {
            foreach (var objectType in _entityTypes)
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
