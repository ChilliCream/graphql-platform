using System.Collections.Immutable;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion.Types.Completion;

public sealed class CompositeSchemaContext
{
    private readonly Dictionary<ITypeNode, ICompositeType> _compositeTypes = new(SyntaxComparer.BySyntax);
    private readonly Dictionary<string, ICompositeNamedType> _typeNameLookup;
    private ImmutableDictionary<string, ITypeDefinitionNode> _typeDefinitions;
    private readonly Dictionary<string, FusionDirectiveDefinition> _directiveTypeNameLookup;
    private ImmutableDictionary<string, DirectiveDefinitionNode> _directiveDefinitions;

    public CompositeSchemaContext(
        string queryType,
        string? mutationType,
        string? subscriptionType,
        IReadOnlyList<DirectiveNode> directives,
        ImmutableArray<ICompositeNamedType> types,
        ImmutableDictionary<string, ITypeDefinitionNode> typeDefinitions,
        ImmutableArray<FusionDirectiveDefinition> directiveTypes,
        ImmutableDictionary<string, DirectiveDefinitionNode> directiveDefinitions)
    {
        _typeNameLookup = types.ToDictionary(t => t.Name);
        _directiveTypeNameLookup = directiveTypes.ToDictionary(t => t.Name);
        _typeDefinitions = typeDefinitions;
        _directiveDefinitions = directiveDefinitions;

        QueryType = queryType;
        MutationType = mutationType;
        SubscriptionType = subscriptionType;
        Types = types;
        Directives = directives;
        DirectiveTypes = directiveTypes;

        AddSpecDirectives();
    }

    public string QueryType { get; }

    public string? MutationType { get; }

    public string? SubscriptionType { get; }

    public ImmutableArray<ICompositeNamedType> Types { get; private set; }

    public IReadOnlyList<DirectiveNode> Directives { get; }

    public ImmutableArray<FusionDirectiveDefinition> DirectiveTypes { get; private set; }

    public T GetTypeDefinition<T>(string typeName)
        where T : ITypeDefinitionNode
    {
        if (_typeDefinitions.TryGetValue(typeName, out var typeDefinition))
        {
            return (T)typeDefinition;
        }

        throw new InvalidOperationException();
    }

    public DirectiveDefinitionNode GetDirectiveDefinition(string typeName)
    {
        if (_directiveDefinitions.TryGetValue(typeName, out var directiveDefinition))
        {
            return directiveDefinition;
        }

        throw new InvalidOperationException();
    }

    public T GetType<T>(string typeName)
        where T : ICompositeNamedType
    {
        if (_typeNameLookup.TryGetValue(typeName, out var type)
            && type is T castedType)
        {
            return castedType;
        }

        throw new InvalidOperationException();
    }

    public ICompositeType GetType(ITypeNode typeStructure, string? typeName = null)
    {
        typeName ??= typeStructure.NamedType().Name.Value;

        if (!_compositeTypes.TryGetValue(typeStructure, out var type))
        {
            type = CreateType(typeStructure, typeName);
            _compositeTypes[typeStructure] = type;
        }

        return type;
    }

    private ICompositeType CreateType(ITypeNode typeNode, string typeName)
    {
        if (!_typeNameLookup.TryGetValue(typeName, out var type))
        {
            if (!IsSpecScalarType(typeName))
            {
                throw new InvalidOperationException("The specified type does not exist.");
            }

            type = CreateSpecScalar(typeName);
            _typeNameLookup[typeName] = type;
        }

        return CreateType(typeNode, type);
    }

    private CompositeScalarType CreateSpecScalar(string name)
    {
        var type = new CompositeScalarType(name, null);
        var typeDef = new ScalarTypeDefinitionNode(null, new NameNode(name), null, Array.Empty<DirectiveNode>());
        type.Complete(new CompositeScalarTypeCompletionContext(DirectiveCollection.Empty));

        _typeDefinitions = _typeDefinitions.SetItem(name, typeDef);
        Types = Types.Add(type);

        return type;
    }

    private static ICompositeType CreateType(ITypeNode typeNode, ICompositeNamedType compositeNamedType)
    {
        if (typeNode is NonNullTypeNode nonNullType)
        {
            return new CompositeNonNullType(CreateType(nonNullType.InnerType(), compositeNamedType));
        }

        if (typeNode is ListTypeNode listType)
        {
            return new CompositeListType(CreateType(listType.Type, compositeNamedType));
        }

        return compositeNamedType;
    }

    public FusionDirectiveDefinition GetDirectiveType(string name)
    {
        if (_directiveTypeNameLookup.TryGetValue(name, out var type))
        {
            return type;
        }

        throw new InvalidOperationException();
    }

    private void AddSpecDirectives()
    {
        var directive = CreateSkipDirective();
        _directiveTypeNameLookup.Add(directive.Name, directive);
        DirectiveTypes = DirectiveTypes.Add(directive);

        directive = CreateIncludeDirective();
        _directiveTypeNameLookup.Add(directive.Name, directive);
        DirectiveTypes = DirectiveTypes.Add(directive);
    }

    private FusionDirectiveDefinition CreateSkipDirective()
    {
        var ifField = new CompositeInputField(
            "if",
            "Skips this field or fragment when the condition is true.",
            defaultValue: null,
            isDeprecated: false,
            deprecationReason: null);

        var skipDirective = new FusionDirectiveDefinition(
            "skip",
            "Directs the executor to skip this field or fragment when the `if` argument is true.",
            isRepeatable: false,
            new CompositeInputFieldCollection([ifField]),
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

        _directiveDefinitions = _directiveDefinitions.SetItem("skip", skipDirectiveDef);

        return skipDirective;
    }

    private FusionDirectiveDefinition CreateIncludeDirective()
    {
        var ifField = new CompositeInputField(
            "if",
            "Includes this field or fragment when the condition is true.",
            defaultValue: null,
            isDeprecated: false,
            deprecationReason: null);

        var includeDirective = new FusionDirectiveDefinition(
            "include",
            "Directs the executor to include this field or fragment when the `if` argument is true.",
            isRepeatable: false,
            new CompositeInputFieldCollection([ifField]),
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

        _directiveDefinitions = _directiveDefinitions.Add("include", includeDirectiveDef);

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
