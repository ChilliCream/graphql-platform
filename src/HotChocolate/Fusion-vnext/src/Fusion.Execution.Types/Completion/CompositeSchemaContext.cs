using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Directives;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;
using HotChocolate.Types;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion.Types.Completion;

internal sealed class CompositeSchemaBuilderContext : ICompositeSchemaBuilderContext
{
#pragma warning disable IDE0052 // WIP
    private readonly DocumentNode _document;
#pragma warning restore IDE0052
    private readonly Dictionary<ITypeNode, IType> _compositeTypes = new(SyntaxComparer.BySyntax);
    private readonly Dictionary<string, IFusionTypeDefinition> _typeDefinitionLookup;
    private ImmutableDictionary<string, ITypeDefinitionNode> _typeDefinitionNodeLookup;
    private readonly Dictionary<string, FusionDirectiveDefinition> _directiveDefinitionLookup;
    private ImmutableDictionary<string, DirectiveDefinitionNode> _directiveDefinitionNodeLookup;
    private readonly List<INeedsCompletion> _completions = [];
    private readonly ImmutableDictionary<string, ImmutableDictionary<SchemaKey, ImmutableHashSet<string>>> _sourceUnions;

    public CompositeSchemaBuilderContext(
        DocumentNode document,
        string name,
        string? description,
        IServiceProvider services,
        string queryType,
        string? mutationType,
        string? subscriptionType,
        ImmutableArray<DirectiveNode> directives,
        ImmutableArray<IFusionTypeDefinition> typeDefinitions,
        ImmutableDictionary<string, ITypeDefinitionNode> typeDefinitionNodeLookup,
        ImmutableArray<FusionDirectiveDefinition> directiveDefinitions,
        ImmutableDictionary<string, DirectiveDefinitionNode> directiveDefinitionNodeLookup,
        ImmutableDictionary<string, SourceSchemaInfo> sourceSchemaLookup,
        IFeatureCollection features,
        CompositeTypeInterceptor interceptor)
    {
        _document = document;
        _sourceUnions = UnionMemberDirectiveParser.Parse(document.Definitions.OfType<UnionTypeDefinitionNode>());

        _typeDefinitionLookup = typeDefinitions.ToDictionary(t => t.Name);
        _directiveDefinitionLookup = directiveDefinitions.ToDictionary(t => t.Name);
        _typeDefinitionNodeLookup = typeDefinitionNodeLookup;
        _directiveDefinitionNodeLookup = directiveDefinitionNodeLookup;

        Name = name;
        Description = description;
        Services = services;
        QueryType = queryType;
        MutationType = mutationType;
        SubscriptionType = subscriptionType;
        Directives = directives;
        TypeDefinitions = typeDefinitions;
        DirectiveDefinitions = directiveDefinitions;
        SourceSchemaLookup = sourceSchemaLookup;
        Features = features;
        Interceptor = interceptor;

        AddSpecDirectives();
    }

    public string Name { get; }

    public string? Description { get; }

    public IServiceProvider Services { get; }

    public CompositeTypeInterceptor Interceptor { get; }

    public string QueryType { get; }

    public string? MutationType { get; }

    public string? SubscriptionType { get; }

    public ImmutableArray<IFusionTypeDefinition> TypeDefinitions { get; private set; }

    public ImmutableArray<DirectiveNode> Directives { get; }

    public ImmutableArray<FusionDirectiveDefinition> DirectiveDefinitions { get; private set; }

    public ImmutableDictionary<string, SourceSchemaInfo> SourceSchemaLookup { get; }

    public IFeatureCollection Features { get; }

    public void RegisterForCompletion(INeedsCompletion completion)
        => _completions.Add(completion);

    public void RegisterForCompletionRange(IEnumerable<INeedsCompletion> completion)
        => _completions.AddRange(completion);

    public void Complete(FusionSchemaDefinition schema)
    {
        foreach (var completion in _completions)
        {
            completion.Complete(schema, this);
        }
    }

    public T GetTypeDefinition<T>(string typeName)
        where T : ITypeDefinitionNode
    {
        if (_typeDefinitionNodeLookup.TryGetValue(typeName, out var typeDefinition))
        {
            return (T)typeDefinition;
        }

        throw new InvalidOperationException();
    }

    public DirectiveDefinitionNode GetDirectiveDefinition(string typeName)
    {
        if (_directiveDefinitionNodeLookup.TryGetValue(typeName, out var directiveDefinition))
        {
            return directiveDefinition;
        }

        throw new InvalidOperationException();
    }

    public T GetType<T>(string typeName)
        where T : ITypeDefinition
    {
        if (_typeDefinitionLookup.TryGetValue(typeName, out var type)
            && type is T castedType)
        {
            return castedType;
        }

        throw new InvalidOperationException();
    }

    public IType GetType(ITypeNode typeStructure, string? typeName = null)
    {
        typeName ??= typeStructure.NamedType().Name.Value;

        if (!_compositeTypes.TryGetValue(typeStructure, out var type))
        {
            type = CreateType(typeStructure, typeName);
            _compositeTypes[typeStructure] = type;
        }

        return type;
    }

    private IType CreateType(ITypeNode typeNode, string typeName)
    {
        if (!_typeDefinitionLookup.TryGetValue(typeName, out var type))
        {
            if (!SpecScalarNames.IsSpecScalar(typeName))
            {
                throw new InvalidOperationException(
                    $"The specified type `{typeName}` does not exist.");
            }

            type = CreateSpecScalar(typeName);
            _typeDefinitionLookup[typeName] = type;
        }

        return CreateType(typeNode, type);
    }

    private FusionScalarTypeDefinition CreateSpecScalar(string name)
    {
        var type = new FusionScalarTypeDefinition(name, null, isInaccessible: false);
        var typeDef = new ScalarTypeDefinitionNode(null, new NameNode(name), null, []);
        type.Complete(new CompositeScalarTypeCompletionContext(
            default,
            FusionDirectiveCollection.Empty,
            specifiedBy: null,
            serializationType: ScalarSerializationType.String,
            pattern: null));

        _typeDefinitionNodeLookup = _typeDefinitionNodeLookup.SetItem(name, typeDef);
        TypeDefinitions = TypeDefinitions.Add(type);

        return type;
    }

    private static IType CreateType(ITypeNode typeNode, ITypeDefinition compositeNamedType)
    {
        if (typeNode is NonNullTypeNode nonNullType)
        {
            return new NonNullType(CreateType(nonNullType.InnerType(), compositeNamedType));
        }

        if (typeNode is ListTypeNode listType)
        {
            return new ListType(CreateType(listType.Type, compositeNamedType));
        }

        return compositeNamedType;
    }

    public FusionDirectiveDefinition GetDirectiveType(string name)
        => _directiveDefinitionLookup.TryGetValue(name, out var type)
            ? type
            : throw new InvalidOperationException();

    public string GetSchemaName(SchemaKey schemaKey)
        => SourceSchemaLookup[schemaKey.Value].Name;

    public ImmutableDictionary<SchemaKey, ImmutableHashSet<string>> GetSourceUnionMembers(
        string objectTypeName)
    {
        return _sourceUnions.TryGetValue(objectTypeName, out var sourceUnions)
            ? sourceUnions
#if NET10_0_OR_GREATER
            : [];
#else
            : ImmutableDictionary<SchemaKey, ImmutableHashSet<string>>.Empty;
#endif
    }

    private void AddSpecDirectives()
    {
        var directive = CreateSkipDirective();
        _directiveDefinitionLookup.Add(directive.Name, directive);
        DirectiveDefinitions = DirectiveDefinitions.Add(directive);

        directive = CreateIncludeDirective();
        _directiveDefinitionLookup.Add(directive.Name, directive);
        DirectiveDefinitions = DirectiveDefinitions.Add(directive);

        directive = CreateSpecifiedByDirective();
        _directiveDefinitionLookup.Add(directive.Name, directive);
        DirectiveDefinitions = DirectiveDefinitions.Add(directive);

        directive = CreateOneOfDirective();
        _directiveDefinitionLookup.Add(directive.Name, directive);
        DirectiveDefinitions = DirectiveDefinitions.Add(directive);
    }

    private FusionDirectiveDefinition CreateSkipDirective()
    {
        var ifField = new FusionInputFieldDefinition(
            0,
            DirectiveNames.Skip.Arguments.If,
            "Skips this field or fragment when the condition is true.",
            defaultValue: null,
            isDeprecated: false,
            deprecationReason: null,
            isInaccessible: false);

        var skipDirective = new FusionDirectiveDefinition(
            DirectiveNames.Skip.Name,
            "Directs the executor to skip this field or fragment when the `if` argument is true.",
            isRepeatable: false,
            new FusionInputFieldDefinitionCollection([ifField]),
            DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment);

        var skipDirectiveDef = new DirectiveDefinitionNode(
            null,
            new NameNode(DirectiveNames.Skip.Name),
            new StringValueNode("Directs the executor to skip this field or fragment when the `if` argument is true."),
            isRepeatable: false,
            [
                new InputValueDefinitionNode(
                    null,
                    new NameNode(DirectiveNames.Skip.Arguments.If),
                    new StringValueNode("Skips this field or fragment when the condition is true."),
                    new NonNullTypeNode(new NamedTypeNode(new NameNode("Boolean"))),
                    null,
                    [])
            ],
            [
                new NameNode(HotChocolate.Language.DirectiveLocation.Field.Value),
                new NameNode(HotChocolate.Language.DirectiveLocation.FragmentSpread.Value),
                new NameNode(HotChocolate.Language.DirectiveLocation.InlineFragment.Value)
            ]);

        _directiveDefinitionNodeLookup = _directiveDefinitionNodeLookup.SetItem(skipDirective.Name, skipDirectiveDef);

        return skipDirective;
    }

    private FusionDirectiveDefinition CreateIncludeDirective()
    {
        var ifField = new FusionInputFieldDefinition(
            0,
            DirectiveNames.Include.Arguments.If,
            "Includes this field or fragment when the condition is true.",
            defaultValue: null,
            isDeprecated: false,
            deprecationReason: null,
            isInaccessible: false);

        var includeDirective = new FusionDirectiveDefinition(
            DirectiveNames.Include.Name,
            "Directs the executor to include this field or fragment when the `if` argument is true.",
            isRepeatable: false,
            new FusionInputFieldDefinitionCollection([ifField]),
            DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment);

        var includeDirectiveDef = new DirectiveDefinitionNode(
            null,
            new NameNode(DirectiveNames.Include.Name),
            new StringValueNode("Directs the executor to include this field or fragment when the `if` argument is true."),
            isRepeatable: false,
            [
                new InputValueDefinitionNode(
                    null,
                    new NameNode(DirectiveNames.Include.Arguments.If),
                    new StringValueNode("Includes this field or fragment when the condition is true."),
                    new NonNullTypeNode(new NamedTypeNode(new NameNode("Boolean"))),
                    null,
                    Array.Empty<DirectiveNode>())
            ],
            [
                new NameNode(HotChocolate.Language.DirectiveLocation.Field.Value),
                new NameNode(HotChocolate.Language.DirectiveLocation.FragmentSpread.Value),
                new NameNode(HotChocolate.Language.DirectiveLocation.InlineFragment.Value)
            ]);

        _directiveDefinitionNodeLookup = _directiveDefinitionNodeLookup.Add(includeDirective.Name, includeDirectiveDef);

        return includeDirective;
    }

    private FusionDirectiveDefinition CreateSpecifiedByDirective()
    {
        var urlField = new FusionInputFieldDefinition(
            0,
            DirectiveNames.SpecifiedBy.Arguments.Url,
            "The specifiedBy URL points to a human-readable specification. This field will only read a result for scalar types.",
            defaultValue: null,
            isDeprecated: false,
            deprecationReason: null,
            isInaccessible: false);

        var specifiedByDirective = new FusionDirectiveDefinition(
            DirectiveNames.SpecifiedBy.Name,
            "The `@specifiedBy` directive is used within the type system definition language to provide a URL for specifying the behavior of custom scalar definitions.",
            isRepeatable: false,
            new FusionInputFieldDefinitionCollection([urlField]),
            DirectiveLocation.Scalar);

        var specifiedByDirectiveDef = new DirectiveDefinitionNode(
            null,
            new NameNode(DirectiveNames.SpecifiedBy.Name),
            new StringValueNode("The `@specifiedBy` directive is used within the type system definition language to provide a URL for specifying the behavior of custom scalar definitions."),
            isRepeatable: false,
            [
                new InputValueDefinitionNode(
                    null,
                    new NameNode(DirectiveNames.SpecifiedBy.Arguments.Url),
                    new StringValueNode("The specifiedBy URL points to a human-readable specification. This field will only read a result for scalar types."),
                    new NonNullTypeNode(new NamedTypeNode(new NameNode("String"))),
                    null,
                    Array.Empty<DirectiveNode>())
            ],
            [
                new NameNode(HotChocolate.Language.DirectiveLocation.Scalar.Value)
            ]);

        _directiveDefinitionNodeLookup = _directiveDefinitionNodeLookup.Add(specifiedByDirective.Name, specifiedByDirectiveDef);

        return specifiedByDirective;
    }

    private FusionDirectiveDefinition CreateOneOfDirective()
    {
        var oneOfDirective = new FusionDirectiveDefinition(
            DirectiveNames.OneOf.Name,
            "The `@oneOf` directive is used within the type system definition language to indicate that an Input Object is a OneOf Input Object.",
            isRepeatable: false,
            new FusionInputFieldDefinitionCollection([]),
            DirectiveLocation.InputObject);

        var oneOfDirectiveDef = new DirectiveDefinitionNode(
            null,
            new NameNode(DirectiveNames.OneOf.Name),
            new StringValueNode("The `@oneOf` directive is used within the type system definition language to indicate that an Input Object is a OneOf Input Object."),
            isRepeatable: false,
            [],
            [
                new NameNode(HotChocolate.Language.DirectiveLocation.InputObject.Value)
            ]);

        _directiveDefinitionNodeLookup = _directiveDefinitionNodeLookup.Add(oneOfDirective.Name, oneOfDirectiveDef);

        return oneOfDirective;
    }
}
