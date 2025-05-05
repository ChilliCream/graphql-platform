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

    public SchemaTypes(SchemaTypesConfiguration configuration)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (configuration.Types is null || configuration.DirectiveTypes is null)
        {
            throw new ArgumentException(
                SchemaTypes_DefinitionInvalid,
                nameof(configuration));
        }

        _types = configuration.Types.ToFrozenDictionary(t => t.Name, StringComparer.Ordinal);
        _possibleTypes = CreatePossibleTypeLookup(configuration.Types).ToFrozenDictionary(StringComparer.Ordinal);
        QueryType = configuration.QueryType!;
        MutationType = configuration.MutationType;
        SubscriptionType = configuration.SubscriptionType;
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


}
