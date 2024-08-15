using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace HotChocolate.Internal;

internal sealed class TypeCache
{
    private readonly object _sync = new();
    private readonly Dictionary<ExtendedTypeId, ExtendedType> _types = new();
    private readonly Dictionary<object, ExtendedType> _typeMemberLookup = new();
    private readonly Dictionary<ExtendedTypeId, TypeInfo> _typeInfos = new();

    public ExtendedType GetType(ExtendedTypeId id)
    {
        lock (_sync)
        {
            return _types[id];
        }
    }

    public bool TryGetType(
        ExtendedTypeId id,
        [NotNullWhen(true)] out ExtendedType? extendedType)
    {
        lock (_sync)
        {
            return _types.TryGetValue(id, out extendedType);
        }
    }

    public bool TryGetType(
        Type type,
        [NotNullWhen(true)] out ExtendedType? extendedType)
    {
        lock (_sync)
        {
            return _typeMemberLookup.TryGetValue(type, out extendedType);
        }
    }

    public ExtendedType GetOrCreateType(object member, Func<ExtendedType> create)
    {
        lock (_sync)
        {
            if (!_typeMemberLookup.TryGetValue(member, out var extendedType))
            {
                var type = create();

                if (_types.TryGetValue(type.Id, out extendedType))
                {
                    _typeMemberLookup[member] = extendedType;
                }
                else
                {
                    extendedType = type;
                    _types[extendedType.Id] = extendedType;
                    _typeMemberLookup[member] = extendedType;
                }
            }
            return extendedType;
        }
    }

    public bool TryAdd(ExtendedType extendedType, object? member = null)
    {
        lock (_sync)
        {
            if (!_types.ContainsKey(extendedType.Id))
            {
                _types[extendedType.Id] = extendedType;
                if (member is not null && !_typeMemberLookup.ContainsKey(member))
                {
                    _typeMemberLookup[member] = extendedType;
                }
                return true;
            }
        }
        return false;
    }

    public TypeInfo GetOrCreateTypeInfo(
        IExtendedType extendedType,
        Func<TypeInfo> create)
    {
        var id = ((ExtendedType)extendedType).Id;

        lock (_sync)
        {
            if (!_typeInfos.TryGetValue(id, out var typeInfo))
            {
                typeInfo = create();
                _typeInfos.Add(id, typeInfo);
            }
            return typeInfo;
        }
    }
}
