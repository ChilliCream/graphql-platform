using System;
using System.Collections.Concurrent;

#nullable enable

namespace HotChocolate.Utilities
{
    public class TypeInspector
        : ITypeInfoFactory
    {
        private static readonly ITypeInfoFactory[] _factories =
        {
            new SchemaTypeInfoFactory(),
            new ExtendedTypeInfoFactory()
        };

        private readonly ConcurrentDictionary<IExtendedType, TypeInfo> _cache =
            new ConcurrentDictionary<IExtendedType, TypeInfo>();

        public bool TryCreate(IExtendedType type, out TypeInfo? typeInfo)
        {
            if (!_cache.TryGetValue(type, out typeInfo))
            {
                if (!TryCreateInternal(type, out typeInfo))
                {
                    typeInfo = default;
                    return false;
                }
                _cache.TryAdd(type, typeInfo);
            }
            return true;
        }

        private bool TryCreateInternal(IExtendedType type, out TypeInfo typeInfo)
        {
            foreach (ITypeInfoFactory factory in _factories)
            {
                if (factory.TryCreate(type, out typeInfo))
                {
                    return true;
                }
            }

            typeInfo = default;
            return false;
        }

        public static TypeInspector Default { get; } = new TypeInspector();
    }
}
