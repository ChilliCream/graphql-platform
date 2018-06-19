using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Configuration;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class TypeInspector
    {
        private readonly Dictionary<Type, TypeInfo> _typeInfoCache
            = new Dictionary<Type, TypeInfo>();

        public TypeInfo CreateTypeInfo(Type nativeType)
        {
            return GetOrCreateTypeInfo(nativeType);
        }

        public TypeInfo GetOrCreateTypeInfo(Type nativeType)
        {
            if (!_typeInfoCache.TryGetValue(nativeType, out TypeInfo typeInfo))
            {
                lock (_typeInfoCache)
                {
                    if (!_typeInfoCache.TryGetValue(nativeType, out typeInfo))
                    {
                        if (typeof(IType).IsAssignableFrom(nativeType))
                        {
                            // typeInfo = CreateTypeInfoInternal(nativeType);
                            // _typeInfoCache[nativeType] = typeInfo;
                        }
                    }
                }
            }
            return typeInfo;
        }



        internal static TypeInspector Default { get; } = new TypeInspector();
    }


    internal interface ITypeInfoFactory
    {
        bool TryCreate(Type type, out TypeInfo typeInfo);
    }
}
