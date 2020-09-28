using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace HotChocolate.Internal
{
    internal sealed class TypeCache
    {
        private readonly object _sync = new object();
        private readonly Dictionary<ExtendedTypeId, ExtendedType> _types =
            new Dictionary<ExtendedTypeId, ExtendedType>();

        private readonly Dictionary<object, ExtendedType> _typeMemberLookup =
            new Dictionary<object, ExtendedType>();

        private readonly Dictionary<ExtendedTypeId, TypeInfo> _typeInfos =
            new Dictionary<ExtendedTypeId, TypeInfo>();

        public ExtendedType GetType(ExtendedTypeId id) => _types[id];

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
                if (!_typeMemberLookup.TryGetValue(member, out ExtendedType? extendedType))
                {
                    ExtendedType type = create();

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
            ExtendedTypeId id = ((ExtendedType)extendedType).Id;

            lock (_sync)
            {
                if (!_typeInfos.TryGetValue(id, out TypeInfo? typeInfo))
                {
                    typeInfo = create();
                    _typeInfos.Add(id, typeInfo);
                }
                return typeInfo;
            }
        }
    }
}
