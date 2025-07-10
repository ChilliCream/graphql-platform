using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;
using HotChocolate.Types;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion.Types.Completion;

internal sealed class CompositeSchemaContext
{
    private readonly Dictionary<ITypeNode, IType> _compositeTypes = new(SyntaxComparer.BySyntax);
    private readonly Dictionary<string, ITypeDefinition> _typeDefinitionLookup;
    private ImmutableDictionary<string, ITypeDefinitionNode> _typeDefinitionNodeLookup;
    private readonly Dictionary<string, FusionDirectiveDefinition> _directiveDefinitionLookup;
    private ImmutableDictionary<string, DirectiveDefinitionNode> _directiveDefinitionNodeLookup;

    public CompositeSchemaContext(
        string name,
        string? description,
        IServiceProvider services,
        string queryType,
        string? mutationType,
        string? subscriptionType,
        ImmutableArray<DirectiveNode> directives,
        ImmutableArray<ITypeDefinition> typeDefinitions,
        ImmutableDictionary<string, ITypeDefinitionNode> typeDefinitionNodeLookup,
        ImmutableArray<FusionDirectiveDefinition> directiveDefinitions,
        ImmutableDictionary<string, DirectiveDefinitionNode> directiveDefinitionNodeLookup,
        IFeatureCollection features)
    {
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
        Features = features;

        AddSpecDirectives();
    }

    public string Name { get; }

    public string? Description { get; }

    public IServiceProvider Services { get; }

    public string QueryType { get; }

    public string? MutationType { get; }

    public string? SubscriptionType { get; }

    public ImmutableArray<ITypeDefinition> TypeDefinitions { get; private set; }

    public ImmutableArray<DirectiveNode> Directives { get; }

    public ImmutableArray<FusionDirectiveDefinition> DirectiveDefinitions { get; private set; }

    public IFeatureCollection Features { get; }

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
            if (!IsSpecScalarType(typeName))
            {
                throw new InvalidOperationException("The specified type does not exist.");
            }

            type = CreateSpecScalar(typeName);
            _typeDefinitionLookup[typeName] = type;
        }

        return CreateType(typeNode, type);
    }

    private FusionScalarTypeDefinition CreateSpecScalar(string name)
    {
        var type = new FusionScalarTypeDefinition(name, null);
        var typeDef = new ScalarTypeDefinitionNode(null, new NameNode(name), null, []);
        type.Complete(new CompositeScalarTypeCompletionContext(default, FusionDirectiveCollection.Empty));

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
    {
        if (_directiveDefinitionLookup.TryGetValue(name, out var type))
        {
            return type;
        }

        throw new InvalidOperationException();
    }

    private void AddSpecDirectives()
    {
        var directive = CreateSkipDirective();
        _directiveDefinitionLookup.Add(directive.Name, directive);
        DirectiveDefinitions = DirectiveDefinitions.Add(directive);

        directive = CreateIncludeDirective();
        _directiveDefinitionLookup.Add(directive.Name, directive);
        DirectiveDefinitions = DirectiveDefinitions.Add(directive);
    }

    private FusionDirectiveDefinition CreateSkipDirective()
    {
        var ifField = new FusionInputFieldDefinition(
            "if",
            "Skips this field or fragment when the condition is true.",
            defaultValue: null,
            isDeprecated: false,
            deprecationReason: null);

        var skipDirective = new FusionDirectiveDefinition(
            "skip",
            "Directs the executor to skip this field or fragment when the `if` argument is true.",
            isRepeatable: false,
            new FusionInputFieldDefinitionCollection([ifField]),
            DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment);

        var skipDirectiveDef = new DirectiveDefinitionNode(
            null,
            new NameNode("skip"),
            new StringValueNode("Directs the executor to skip this field or fragment when the `if` argument is true."),
            isRepeatable: false,
            [
                new InputValueDefinitionNode(
                    null,
                    new NameNode("if"),
                    new StringValueNode("Skips this field or fragment when the condition is true."),
                    new NonNullTypeNode(new NamedTypeNode(new NameNode("Boolean"))),
                    null,
                    Array.Empty<DirectiveNode>())
            ],
            [
                new NameNode(Language.DirectiveLocation.Field.Value),
                new NameNode(Language.DirectiveLocation.FragmentSpread.Value),
                new NameNode(Language.DirectiveLocation.InlineFragment.Value)
            ]);

        _directiveDefinitionNodeLookup = _directiveDefinitionNodeLookup.SetItem("skip", skipDirectiveDef);

        return skipDirective;
    }

    private FusionDirectiveDefinition CreateIncludeDirective()
    {
        var ifField = new FusionInputFieldDefinition(
            "if",
            "Includes this field or fragment when the condition is true.",
            defaultValue: null,
            isDeprecated: false,
            deprecationReason: null);

        var includeDirective = new FusionDirectiveDefinition(
            "include",
            "Directs the executor to include this field or fragment when the `if` argument is true.",
            isRepeatable: false,
            new FusionInputFieldDefinitionCollection([ifField]),
            DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment);

        var includeDirectiveDef = new DirectiveDefinitionNode(
            null,
            new NameNode("include"),
            new StringValueNode("Directs the executor to include this field or fragment when the `if` argument is true."),
            isRepeatable: false,
            [
                new InputValueDefinitionNode(
                    null,
                    new NameNode("if"),
                    new StringValueNode("Includes this field or fragment when the condition is true."),
                    new NonNullTypeNode(new NamedTypeNode(new NameNode("Boolean"))),
                    null,
                    Array.Empty<DirectiveNode>())
            ],
            [
                new NameNode(Language.DirectiveLocation.Field.Value),
                new NameNode(Language.DirectiveLocation.FragmentSpread.Value),
                new NameNode(Language.DirectiveLocation.InlineFragment.Value)
            ]);

        _directiveDefinitionNodeLookup = _directiveDefinitionNodeLookup.Add("include", includeDirectiveDef);

        return includeDirective;
    }

    private static bool IsSpecScalarType(string name)
        => name switch
        {
            "ID" => true,
            "String" => true,
            "Int" => true,
            "Float" => true,
            "Boolean" => true,
            _ => false
        };
}
