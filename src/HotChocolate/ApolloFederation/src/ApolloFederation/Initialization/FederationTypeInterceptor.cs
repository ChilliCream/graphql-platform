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
using HotChocolate.Types.Helpers;
using static HotChocolate.ApolloFederation.ThrowHelper;
using static HotChocolate.ApolloFederation.Constants.WellKnownContextData;
using static HotChocolate.Types.TagHelper;

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
    private IDescriptorContext _context = default!;
    private ITypeInspector _typeInspector = default!;
    private ObjectType _queryType = default!;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _typeInspector = context.TypeInspector;
        _context = context;
        ModifyOptions(context, o => o.Mode = TagMode.ApolloFederation);
    }

    public override void OnAfterInitialize(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition)
    {
        if (discoveryContext.Type is ObjectType objectType &&
            definition is ObjectTypeDefinition objectTypeDefinition)
        {
            ApplyMethodLevelReferenceResolvers(
                objectType,
                objectTypeDefinition);

            AddToUnionIfHasTypeLevelKeyDirective(
                objectType,
                objectTypeDefinition);

            AggregatePropertyLevelKeyDirectives(
                objectType,
                objectTypeDefinition,
                discoveryContext);
        }
    }

    internal override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeDefinition definition,
        OperationType operationType)
    {
        if (operationType is OperationType.Query)
        {
            _queryType = (ObjectType) completionContext.Type;
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

        ApplyFederationDirectives(definition);
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
        if (!typeDef.GetContextData().TryGetValue(EntityResolver, out var resolversObject))
        {
            return;
        }

        if (resolversObject is not IReadOnlyList<ReferenceResolverDefinition> resolvers)
        {
            return;
        }

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

    private void AddServiceTypeToQueryType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition)
    {
        if (!ReferenceEquals(completionContext.Type, _queryType))
        {
            return;
        }

        var objectTypeDefinition = (ObjectTypeDefinition)definition!;

        var serviceFieldDescriptor = ObjectFieldDescriptor.New(
            _context,
            WellKnownFieldNames.Service);
        serviceFieldDescriptor
            .Type<NonNullType<ServiceType>>()
            .Resolve(_empty);
        objectTypeDefinition.Fields.Add(serviceFieldDescriptor.CreateDefinition());

        var entitiesFieldDescriptor = ObjectFieldDescriptor.New(
            _context,
            WellKnownFieldNames.Entities);
        entitiesFieldDescriptor
            .Type<NonNullType<ListType<EntityType>>>()
            .Argument(
                WellKnownArgumentNames.Representations,
                descriptor => descriptor.Type<NonNullType<ListType<NonNullType<AnyType>>>>())
            .Resolve(
                c => EntitiesResolver.ResolveAsync(
                    c.Schema,
                    c.ArgumentValue<IReadOnlyList<Representation>>(
                        WellKnownArgumentNames.Representations),
                    c));
        objectTypeDefinition.Fields.Add(entitiesFieldDescriptor.CreateDefinition());
    }

    private void ApplyMethodLevelReferenceResolvers(
        ObjectType objectType,
        ObjectTypeDefinition objectTypeDefinition)
    {
        if (objectType.RuntimeType == typeof(object))
        {
            return;
        }

        var descriptor = ObjectTypeDescriptor.From(_context, objectTypeDefinition);

        foreach (var possibleReferenceResolver in
            objectType.RuntimeType.GetMethods(BindingFlags.Static | BindingFlags.Public))
        {
            if (!possibleReferenceResolver.IsDefined(typeof(ReferenceResolverAttribute)))
            {
                continue;
            }

            foreach (var attribute in possibleReferenceResolver.GetCustomAttributes(true))
            {
                if (attribute is ReferenceResolverAttribute casted)
                {
                    casted.TryConfigure(_context, descriptor, possibleReferenceResolver);
                }
            }
        }

        descriptor.CreateDefinition();
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
        {
            bool foundMarkers = objectTypeDefinition.Fields
                .Any(f => f.ContextData.ContainsKey(KeyMarker));
            if (!foundMarkers)
            {
                return;
            }
        }

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

    /// <summary>
    /// Apply Apollo Federation directives based on the custom attributes.
    /// </summary>
    /// <param name="definition">
    /// Type definition
    /// </param>
    private void ApplyFederationDirectives(DefinitionBase? definition)
    {
        switch (definition)
        {
            case EnumTypeDefinition enumTypeDefinition:
            {
                ApplyEnumDirectives(enumTypeDefinition);
                ApplyEnumValueDirectives(enumTypeDefinition.Values);
                break;
            }
            case InterfaceTypeDefinition interfaceTypeDefinition:
            {
                ApplyInterfaceDirectives(interfaceTypeDefinition);
                ApplyInterfaceFieldDirectives(interfaceTypeDefinition.Fields);
                break;
            }
            case InputObjectTypeDefinition inputObjectTypeDefinition:
            {
                ApplyInputObjectDirectives(inputObjectTypeDefinition);
                ApplyInputFieldDirectives(inputObjectTypeDefinition.Fields);
                break;
            }
            case ObjectTypeDefinition objectTypeDefinition:
            {
                ApplyObjectDirectives(objectTypeDefinition);
                ApplyObjectFieldDirectives(objectTypeDefinition.Fields);
                break;
            }
            case UnionTypeDefinition unionTypeDefinition:
            {
                ApplyUnionDirectives(unionTypeDefinition);
                break;
            }
            default:
                break;
        }
    }

    private void ApplyEnumDirectives(EnumTypeDefinition enumTypeDefinition)
    {
        var requiredScopes = new List<List<Scope>>();
        var descriptor = EnumTypeDescriptor.From(_context, enumTypeDefinition);
        foreach (var attribute in enumTypeDefinition.RuntimeType.GetCustomAttributes(true))
        {
            switch (attribute)
            {
                case ApolloTagAttribute tag:
                {
                    descriptor.ApolloTag(tag.Name);
                    break;
                }
                case ApolloAuthenticatedAttribute:
                {
                    descriptor.ApolloAuthenticated();
                    break;
                }
                case InaccessibleAttribute:
                {
                    descriptor.Inaccessible();
                    break;
                }
                case RequiresScopesAttribute scopes:
                {
                    var addedScopes = scopes.Scopes
                        .Select(scope => new Scope(scope))
                        .ToList();
                    requiredScopes.Add(addedScopes);
                    break;
                }
                default: break;
            }
        }

        if (requiredScopes.Count > 0)
        {
            descriptor.RequiresScopes(requiredScopes);
        }
    }

    private void ApplyEnumValueDirectives(IList<EnumValueDefinition> enumValues)
    {
        foreach (var enumValueDefinition in enumValues)
        {
            if (enumValueDefinition.Member == null)
            {
                continue;
            }

            var enumValueDescriptor = EnumValueDescriptor.From(_context, enumValueDefinition);
            foreach (var attribute in enumValueDefinition.Member.GetCustomAttributes(true))
            {
                switch (attribute)
                {
                    case InaccessibleAttribute:
                    {
                        enumValueDescriptor.Inaccessible();
                        break;
                    }
                    case ApolloTagAttribute casted:
                    {
                        enumValueDescriptor.ApolloTag(casted.Name);
                        break;
                    }
                }
            }
        }
    }

    private void ApplyInterfaceDirectives(InterfaceTypeDefinition interfaceTypeDefinition)
    {
        var descriptor = InterfaceTypeDescriptor.From(_context, interfaceTypeDefinition);
        var requiresScopes = new List<List<Scope>>();
        foreach (var attribute in interfaceTypeDefinition.RuntimeType.GetCustomAttributes(true))
        {
            switch (attribute)
            {
                case ApolloTagAttribute tag:
                {
                    descriptor.ApolloTag(tag.Name);
                    break;
                }
                case ApolloAuthenticatedAttribute:
                {
                    descriptor.ApolloAuthenticated();
                    break;
                }
                case InaccessibleAttribute:
                {
                    descriptor.Inaccessible();
                    break;
                }
                case RequiresScopesAttribute scopes:
                {
                    requiresScopes.Add(scopes.Scopes.Select(scope => new Scope(scope)).ToList());
                    break;
                }
                default: break;
            }
        }

        if (requiresScopes.Count > 0)
        {
            descriptor.RequiresScopes(requiresScopes);
        }
    }

    private void ApplyInterfaceFieldDirectives(IList<InterfaceFieldDefinition> fields)
    {
        foreach (var fieldDefinition in fields)
        {
            var descriptor = InterfaceFieldDescriptor.From(_context, fieldDefinition);
            if (fieldDefinition.Member == null)
            {
                continue;
            }

            var requiresScopes = new List<List<Scope>>();
            foreach (var attribute in fieldDefinition.Member.GetCustomAttributes(true))
            {
                switch (attribute)
                {
                    case ApolloTagAttribute tag:
                    {
                        descriptor.ApolloTag(tag.Name);
                        break;
                    }
                    case ApolloAuthenticatedAttribute:
                    {
                        descriptor.ApolloAuthenticated();
                        break;
                    }
                    case InaccessibleAttribute:
                    {
                        descriptor.Inaccessible();
                        break;
                    }
                    case RequiresScopesAttribute scopes:
                    {
                        requiresScopes.Add(scopes.Scopes.Select(scope => new Scope(scope)).ToList());
                        break;
                    }
                    default: break;
                }
            }

            if (requiresScopes.Count > 0)
            {
                descriptor.RequiresScopes(requiresScopes);
            }
        }
    }

    private void ApplyInputObjectDirectives(InputObjectTypeDefinition inputObjectTypeDefinition)
    {
        var descriptor = InputObjectTypeDescriptor.From(_context, inputObjectTypeDefinition);
        foreach (var attribute in inputObjectTypeDefinition.RuntimeType.GetCustomAttributes(true))
        {
            switch (attribute)
            {
                case InaccessibleAttribute:
                {
                    descriptor.Inaccessible();
                    break;
                }
                case ApolloTagAttribute casted:
                {
                    descriptor.ApolloTag(casted.Name);
                    break;
                }
            }
        }
    }

    private void ApplyInputFieldDirectives(IList<InputFieldDefinition> inputFields)
    {
        foreach (var fieldDefinition in inputFields)
        {
            if (fieldDefinition.RuntimeType == null)
            {
                continue;
            }

            var fieldDescriptor = InputFieldDescriptor.From(_context, fieldDefinition);
            foreach (var attribute in fieldDefinition.RuntimeType.GetCustomAttributes(true))
            {
                switch (attribute)
                {
                    case InaccessibleAttribute:
                    {
                        fieldDescriptor.Inaccessible();
                        break;
                    }
                    case ApolloTagAttribute casted:
                    {
                        fieldDescriptor.ApolloTag(casted.Name);
                        break;
                    }
                }
            }
        }
    }

    private void ApplyObjectDirectives(ObjectTypeDefinition objectTypeDefinition)
    {
        var descriptor = ObjectTypeDescriptor.From(_context, objectTypeDefinition);
        var requiresScopes = new List<List<Scope>>();
        foreach (var attribute in objectTypeDefinition.RuntimeType.GetCustomAttributes(true))
        {
            switch (attribute)
            {
                case ApolloTagAttribute tag:
                {
                    descriptor.ApolloTag(tag.Name);
                    break;
                }
                case ApolloAuthenticatedAttribute:
                {
                    descriptor.ApolloAuthenticated();
                    break;
                }
                case InaccessibleAttribute:
                {
                    descriptor.Inaccessible();
                    break;
                }
                case RequiresScopesAttribute scopes:
                {
                    requiresScopes.Add(scopes.Scopes.Select(scope => new Scope(scope)).ToList());
                    break;
                }
                case ShareableAttribute:
                {
                    descriptor.Shareable();
                    break;
                }
                default: break;
            }
        }

        if (requiresScopes.Count > 0)
        {
            descriptor.RequiresScopes(requiresScopes);
        }
    }

    private void ApplyObjectFieldDirectives(IEnumerable<ObjectFieldDefinition> fields)
    {
        foreach (var fieldDefinition in fields)
        {
            if (fieldDefinition.Member == null)
            {
                continue;
            }

            var requiresScopes = new List<List<Scope>>();
            var descriptor = ObjectFieldDescriptor.From(_context, fieldDefinition);
            foreach (var attribute in fieldDefinition.Member.GetCustomAttributes(true))
            {
                switch (attribute)
                {
                    case ApolloTagAttribute tag:
                    {
                        descriptor.ApolloTag(tag.Name);
                        break;
                    }
                    case ApolloAuthenticatedAttribute:
                    {
                        descriptor.ApolloAuthenticated();
                        break;
                    }
                    case InaccessibleAttribute:
                    {
                        descriptor.Inaccessible();
                        break;
                    }
                    case RequiresScopesAttribute scopes:
                    {
                        requiresScopes.Add(scopes.Scopes.Select(scope => new Scope(scope)).ToList());
                        break;
                    }
                    case ShareableAttribute:
                    {
                        descriptor.Shareable();
                        break;
                    }
                    default: break;
                }
            }

            if (requiresScopes.Count > 0)
            {
                descriptor.RequiresScopes(requiresScopes);
            }
        }
    }

    private void ApplyUnionDirectives(UnionTypeDefinition unionTypeDefinition)
    {
        var descriptor = UnionTypeDescriptor.From(_context, unionTypeDefinition);
        foreach (var attribute in unionTypeDefinition.RuntimeType.GetCustomAttributes(true))
        {
            switch (attribute)
            {
                case InaccessibleAttribute:
                {
                    descriptor.Inaccessible();
                    break;
                }
                case ApolloTagAttribute casted:
                {
                    descriptor.ApolloTag(casted.Name);
                    break;
                }
            }
        }
    }
}
