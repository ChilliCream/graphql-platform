using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace HotChocolate.Internal
{
    internal sealed class TypeCache
    {
        private readonly Dictionary<ExtendedTypeId, ExtendedType> _types =
            new Dictionary<ExtendedTypeId, ExtendedType>();

        private readonly Dictionary<object, ExtendedType> _typeMemberLookup =
            new Dictionary<object, ExtendedType>();

        private readonly Dictionary<ExtendedTypeId, TypeInfo> _typeInfos =
            new Dictionary<ExtendedTypeId, TypeInfo>();

        public ExtendedType GetType(ExtendedTypeId id) => _types[id];

        public bool TryGetType(
            ExtendedTypeId id,
            [NotNullWhen(true)]out ExtendedType? extendedType) =>
            _types.TryGetValue(id, out extendedType);

        public bool TryGetType(
            Type type,
            [NotNullWhen(true)]out ExtendedType? extendedType) =>
            _typeMemberLookup.TryGetValue(type, out extendedType);

        public ExtendedType GetOrCreateType(object member, Func<ExtendedType> create)
        {
            if (!_typeMemberLookup.TryGetValue(member, out ExtendedType? extendedType))
            {
                lock (_typeMemberLookup)
                {
                    if (!_typeMemberLookup.TryGetValue(member, out extendedType))
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
                }
            }
            return extendedType;
        }

        public bool TryAdd(ExtendedType extendedType, object? member = null)
        {
            lock (_typeMemberLookup)
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

            if (!_typeInfos.TryGetValue(id, out TypeInfo? typeInfo))
            {
                lock (_typeInfos)
                {
                    if (!_typeInfos.TryGetValue(id, out typeInfo))
                    {
                        typeInfo = create();
                        _typeInfos.Add(id, typeInfo);
                    }
                }
            }
            return typeInfo;
        }

        #if DEBUG

        public IReadOnlyList<IExtendedType> FindType(string phrase)
        {
            var list = new List<IExtendedType>();

            lock (_types)
            {
                foreach (ExtendedType type in _types.Values)
                {
                    if (type.Source.ToString().Contains(phrase))
                    {
                        if (!list.Contains(type))
                        {
                            list.Add(type);
                        }
                    }
                }
            }

            return list;
        }

        public IReadOnlyList<TypeInfo> FindTypeInfo(string phrase)
        {
            var list = new List<TypeInfo>();

            lock (_typeInfos)
            {
                foreach (TypeInfo typeInfo in _typeInfos.Values)
                {
                    if (typeInfo.GetExtendedType().Source.ToString().Contains(phrase))
                    {
                        if (!list.Contains(typeInfo))
                        {
                            list.Add(typeInfo);
                        }
                    }
                }
            }

            return list;
        }

        #endif
    }
}
