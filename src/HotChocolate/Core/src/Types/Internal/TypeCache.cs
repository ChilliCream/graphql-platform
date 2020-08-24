using System;
using System.Collections.Generic;
using HotChocolate.Utilities;

namespace HotChocolate.Internal
{
    internal static class TypeCache
    {
        private static readonly Dictionary<object, ExtendedType> _types =
            new Dictionary<object, ExtendedType>();

        private static readonly Dictionary<IExtendedType, TypeInfo> _typeInfos =
            new Dictionary<IExtendedType, TypeInfo>();

        public static ExtendedType GetOrCreateType(object member, Func<ExtendedType> create)
        {
            if (!_types.TryGetValue(member, out ExtendedType? extendedType))
            {
                lock (_types)
                {
                    if (!_types.TryGetValue(member, out extendedType))
                    {
                        ExtendedType type = create();
                        Type identity = ExtendedTypeRewriter.Rewrite(type);

                        if (_types.TryGetValue(identity, out extendedType))
                        {
                            _types.Add(member, extendedType);
                        }
                        else
                        {
                            extendedType = type;
                            _types[member] = extendedType;
                            _types[identity] = extendedType;
                        }
                    }
                }
            }

            return extendedType;
        }

        public static ExtendedType GetOrCreateType(Func<ExtendedType> create)
        {
            lock (_types)
            {
                ExtendedType type = create();
                Type identity = ExtendedTypeRewriter.Rewrite(type);

                if (!_types.TryGetValue(identity, out ExtendedType? extendedType))
                {
                    extendedType = type;
                    _types.Add(identity, extendedType);
                }

                return extendedType;
            }
        }

        public static TypeInfo GetOrCreateTypeInfo(
            IExtendedType extendedType,
            Func<TypeInfo> create)
        {
            if (!_typeInfos.TryGetValue(extendedType, out TypeInfo? typeInfo))
            {
                lock (_typeInfos)
                {
                    if (!_typeInfos.TryGetValue(extendedType, out typeInfo))
                    {
                        typeInfo = create();
                        _typeInfos.Add(extendedType, typeInfo);
                    }
                }
            }
            return typeInfo;
        }

        public static IReadOnlyList<IExtendedType> FindType(string phrase)
        {
            var list = new List<IExtendedType>();

            lock (_types)
            {
                foreach (IExtendedType type in _types.Values)
                {
                    if (type.ToString().Contains(phrase))
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

        public static IReadOnlyList<TypeInfo> FindTypeInfo(string phrase)
        {
            var list = new List<TypeInfo>();

            lock (_typeInfos)
            {
                foreach (TypeInfo typeInfo in _typeInfos.Values)
                {
                    if (typeInfo.GetExtendedType().ToString().Contains(phrase))
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
    }
}
