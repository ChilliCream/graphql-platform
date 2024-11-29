using System.Collections.Immutable;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Completion;

public sealed class CompositeSchemaContext(
    string queryType,
    string? mutationType,
    string? subscriptionType,
    IReadOnlyList<DirectiveNode> directives,
    ImmutableArray<ICompositeNamedType> types,
    ImmutableDictionary<string, ITypeDefinitionNode> typeDefinitions)
{
    private readonly Dictionary<ITypeNode, ICompositeType> _compositeTypes = new(SyntaxComparer.BySyntax);
    private readonly Dictionary<string, ICompositeNamedType> _typeNameLookup = types.ToDictionary(t => t.Name);

    public string QueryType { get; } = queryType;

    public string? MutationType { get; } = mutationType;

    public string? SubscriptionType { get; } = subscriptionType;

    public ImmutableArray<ICompositeNamedType> Types { get; private set; } = types;

    public IReadOnlyList<DirectiveNode> Directives { get; } = directives;

    public ImmutableArray<CompositeDirectiveDefinition> DirectiveDefinitions { get; } =
        ImmutableArray<CompositeDirectiveDefinition>.Empty;

    public T GetTypeDefinition<T>(string typeName)
        where T : ITypeDefinitionNode
    {
        if (typeDefinitions.TryGetValue(typeName, out var typeDefinition))
        {
            return (T)typeDefinition;
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

    private ICompositeNamedType CreateSpecScalar(string name)
    {
        var type = new CompositeScalarType(name, null);
        var typeDef = new ScalarTypeDefinitionNode(null, new NameNode(name), null, Array.Empty<DirectiveNode>());
        type.Complete(new CompositeScalarTypeCompletionContext(DirectiveCollection.Empty));

        typeDefinitions = typeDefinitions.SetItem(name, typeDef);
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

    public CompositeDirectiveDefinition GetDirectiveDefinition(string name)
    {
        throw new NotImplementedException();
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
