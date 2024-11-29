using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class CompositeSchema
{
    private readonly FrozenDictionary<string, ICompositeNamedType> _types;
    private readonly FrozenDictionary<string, CompositeDirectiveDefinition> _directiveDefinitions;

    public CompositeSchema(
        string? description,
        CompositeObjectType queryType,
        CompositeObjectType? mutationType,
        CompositeObjectType? subscriptionType,
        FrozenDictionary<string, ICompositeNamedType> types,
        DirectiveCollection directives,
        FrozenDictionary<string, CompositeDirectiveDefinition> directiveDefinitions)
    {
        Description = description;
        QueryType = queryType;
        MutationType = mutationType;
        SubscriptionType = subscriptionType;
        _types = types;
        Directives = directives;
        _directiveDefinitions = directiveDefinitions;
    }

    public string? Description { get; }

    /// <summary>
    /// The type that query operations will be rooted at.
    /// </summary>
    public CompositeObjectType QueryType { get; }

    /// <summary>
    /// If this server supports mutation, the type that
    /// mutation operations will be rooted at.
    /// </summary>
    public CompositeObjectType? MutationType { get; }

    /// <summary>
    /// If this server support subscription, the type that
    /// subscription operations will be rooted at.
    /// </summary>
    public CompositeObjectType? SubscriptionType { get; }

    /// <summary>
    /// Gets all the schema types.
    /// </summary>
    public ImmutableArray<ICompositeNamedType> Types => _types.Values;

    public DirectiveCollection Directives { get; }

    /// <summary>
    /// Gets all the directive types that are supported by this schema.
    /// </summary>
    public ImmutableArray<CompositeDirectiveDefinition> DirectiveDefinitions
        => _directiveDefinitions.Values;

    /// <summary>
    /// Gets a type by its name and kind.
    /// </summary>
    /// <param name="typeName">The name of the type.</param>
    /// <returns>The type.</returns>
    /// <exception cref="ArgumentException">
    /// The specified type does not exist or is not of the
    /// specified type kind.
    /// </exception>
    [return: NotNull]
    public ICompositeNamedType GetType(string typeName)
        => GetType<ICompositeNamedType>(typeName);

    /// <summary>
    /// Gets a type by its name and kind.
    /// </summary>
    /// <typeparam name="T">The expected type kind.</typeparam>
    /// <param name="typeName">The name of the type.</param>
    /// <returns>The type.</returns>
    /// <exception cref="ArgumentException">
    /// The specified type does not exist or is not of the
    /// specified type kind.
    /// </exception>
    [return: NotNull]
    public T GetType<T>(string typeName)
        where T : ICompositeNamedType
    {
        if (_types.TryGetValue(typeName, out var resolvedType)
            && resolvedType is T castedType)
        {
            return castedType;
        }

        throw new ArgumentException(
            $"The specified type `{typeName}` does not exist or is not of the specified type kind.",
            nameof(typeName));
    }

    public CompositeObjectType GetOperationType(OperationType operation)
    {
        var operationType = operation switch
        {
            OperationType.Query => QueryType,
            OperationType.Mutation => MutationType,
            OperationType.Subscription => SubscriptionType,
            _ => null
        };

        if (operationType is null)
        {
            throw new InvalidOperationException();
        }

        return operationType;
    }

    /// <summary>
    /// Tries to get a type by its name and kind.
    /// </summary>
    /// <typeparam name="T">The expected type kind.</typeparam>
    /// <param name="typeName">The name of the type.</param>
    /// <param name="type">The resolved type.</param>
    /// <returns>
    /// <c>true</c>, if a type with the name exists and is of the specified
    /// kind, <c>false</c> otherwise.
    /// </returns>
    public bool TryGetType<T>(string typeName, [MaybeNullWhen(false)] out T type)
        where T : ICompositeNamedType
    {
        if (_types.TryGetValue(typeName, out var resolvedType)
            && resolvedType is T castedType)
        {
            type = castedType;
            return true;
        }

        type = default;
        return false;
    }

    /// <summary>
    /// Gets the possible object types to
    /// an abstract type (union type or interface type).
    /// </summary>
    /// <param name="abstractType">The abstract type.</param>
    /// <returns>
    /// Returns a collection with all possible object types
    /// for the given abstract type.
    /// </returns>
    public ImmutableArray<CompositeObjectType> GetPossibleTypes(ICompositeNamedType abstractType)
    {
        if(abstractType.Kind is not TypeKind.Union and not TypeKind.Interface and not TypeKind.Object)
        {
            throw new ArgumentException(
                "The specified type is not an abstract type.",
                nameof(abstractType));
        }

        if (abstractType is CompositeUnionType unionType)
        {
            return unionType.Types;
        }

        if (abstractType is CompositeInterfaceType interfaceType)
        {
            var builder = ImmutableArray.CreateBuilder<CompositeObjectType>();

            foreach (var type in _types.Values)
            {
                if (type is CompositeObjectType obj)
                {
                    if (obj.Implements.ContainsName(interfaceType.Name))
                    {
                        builder.Add(obj);
                    }
                }
            }

            return builder.ToImmutable();
        }

        if(abstractType is CompositeObjectType objectType)
        {
            return [objectType];
        }

        return [];
    }

    /// <summary>
    /// Gets a directive type by its name.
    /// </summary>
    /// <param name="directiveName">
    /// The directive name.
    /// </param>
    /// <returns>
    /// Returns directive type that was resolved by the given name
    /// or <c>null</c> if there is no directive with the specified name.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The specified directive type does not exist.
    /// </exception>
    public CompositeDirectiveDefinition GetDirectiveType(string directiveName)
    {
        if (_directiveDefinitions.TryGetValue(directiveName, out var directiveType))
        {
            return directiveType;
        }

        throw new ArgumentException(
            $"The specified directive type `{directiveName}` does not exist.",
            nameof(directiveName));
    }

    /// <summary>
    /// Tries to get a directive type by its name.
    /// </summary>
    /// <param name="directiveName">
    /// The directive name.
    /// </param>
    /// <param name="directiveType">
    /// The directive type that was resolved by the given name
    /// or <c>null</c> if there is no directive with the specified name.
    /// </param>
    /// <returns>
    /// <c>true</c>, if a directive type with the specified
    /// name exists; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetDirectiveType(
        string directiveName,
        [NotNullWhen(true)] out CompositeDirectiveDefinition? directiveType)
        => _directiveDefinitions.TryGetValue(directiveName, out directiveType);
}
