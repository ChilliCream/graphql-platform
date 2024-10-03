#nullable enable

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate;

internal sealed class SchemaTypes
{
    private readonly FrozenDictionary<string, INamedType> _types;
    private readonly FrozenDictionary<string, List<ObjectType>> _possibleTypes;

    public SchemaTypes(SchemaTypesDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (definition.Types is null || definition.DirectiveTypes is null)
        {
            throw new ArgumentException(
                SchemaTypes_DefinitionInvalid,
                nameof(definition));
        }

        _types = definition.Types.ToFrozenDictionary(t => t.Name, StringComparer.Ordinal);
        _possibleTypes = CreatePossibleTypeLookup(definition.Types).ToFrozenDictionary(StringComparer.Ordinal);
        QueryType = definition.QueryType!;
        MutationType = definition.MutationType;
        SubscriptionType = definition.SubscriptionType;
    }

    public ObjectType QueryType { get; }

    public ObjectType? MutationType { get; }

    public ObjectType? SubscriptionType { get; }

    public T GetType<T>(string typeName) where T : IType
    {
        if (_types.TryGetValue(typeName, out var namedType)
            && namedType is T type)
        {
            return type;
        }

        throw new ArgumentException(
            string.Format(SchemaTypes_GetType_DoesNotExist, typeName, typeof(T).Name),
            nameof(typeName));
    }

    public bool TryGetType<T>(string typeName, [NotNullWhen(true)] out T? type)
        where T : IType
    {
        if (_types.TryGetValue(typeName, out var namedType)
            && namedType is T t)
        {
            type = t;
            return true;
        }

        type = default;
        return false;
    }

    public IReadOnlyCollection<INamedType> GetTypes()
    {
        return _types.Values;
    }

    public bool TryGetClrType(string typeName, [NotNullWhen(true)] out Type? clrType)
    {
        if (_types.TryGetValue(typeName, out var type)
            && type is IHasRuntimeType ct
            && ct.RuntimeType != typeof(object))
        {
            clrType = ct.RuntimeType;
            return true;
        }

        clrType = null;
        return false;
    }

    public bool TryGetPossibleTypes(
        string abstractTypeName,
        [NotNullWhen(true)] out IReadOnlyList<ObjectType>? types)
    {
        if (_possibleTypes.TryGetValue(abstractTypeName, out var pt))
        {
            types = pt;
            return true;
        }

        types = null;
        return false;
    }

    private static Dictionary<string, List<ObjectType>> CreatePossibleTypeLookup(
        IReadOnlyCollection<INamedType> types)
    {
        var possibleTypes = new Dictionary<string, List<ObjectType>>(StringComparer.Ordinal);

        foreach (var objectType in types.OfType<ObjectType>())
        {
            possibleTypes[objectType.Name] = [objectType,];

            foreach (var interfaceType in objectType.Implements)
            {
                if (!possibleTypes.TryGetValue(interfaceType.Name, out var pt))
                {
                    pt = [];
                    possibleTypes[interfaceType.Name] = pt;
                }

                pt.Add(objectType);
            }
        }

        foreach (var unionType in types.OfType<UnionType>())
        {
            foreach (var objectType in unionType.Types.Values)
            {
                if (!possibleTypes.TryGetValue(
                    unionType.Name, out var pt))
                {
                    pt = [];
                    possibleTypes[unionType.Name] = pt;
                }

                pt.Add(objectType);
            }
        }

        return possibleTypes;
    }
}
