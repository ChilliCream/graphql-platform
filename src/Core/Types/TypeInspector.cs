using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Configuration;

namespace HotChocolate.Types
{
    internal class TypeInspector
    {
        private Dictionary<Type, TypeInfo> _typeInfoCache = new Dictionary<Type, TypeInfo>
        {
            { typeof(string), new TypeInfo(typeof(StringType)) },
            { typeof(Task<string>), new TypeInfo(typeof(StringType)) },
            { typeof(int), new TypeInfo(typeof(IntType),
                t => new NonNullType(new IntType())) },
            { typeof(int?), new TypeInfo(typeof(IntType)) },
            { typeof(Task<int>), new TypeInfo(typeof(IntType),
                t => new NonNullType(new IntType())) },
            { typeof(Task<int?>), new TypeInfo(typeof(IntType)) },
            { typeof(bool), new TypeInfo(typeof(BooleanType),
                t => new NonNullType(new BooleanType())) },
            { typeof(bool?), new TypeInfo(typeof(BooleanType)) },

            { typeof(Task<bool>), new TypeInfo(typeof(BooleanType),
                t => new NonNullType(new BooleanType())) },
            { typeof(Task<bool?>), new TypeInfo(typeof(BooleanType)) }
        };

        public bool IsSupported(Type nativeType)
        {
            try
            {
                TypeInfo typeInfo = GetOrCreateTypeInfo(nativeType);
                return typeInfo.NativeNamedType != null;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        public TypeInfo CreateTypeInfo(Type nativeType)
        {
            return GetOrCreateTypeInfo(nativeType);
        }

        public Type ExtractNamedType(Type nativeType)
        {
            TypeInfo typeInfo = GetOrCreateTypeInfo(nativeType);
            return typeInfo.NativeNamedType;
        }

        public IOutputType CreateOutputType(
            ITypeRegistry typeRegistry, Type nativeType)
        {
            TypeInfo typeInfo = GetOrCreateTypeInfo(nativeType);
            IType type = typeInfo.TypeFactory(typeRegistry);
            if (type.IsOutputType())
            {
                return (IOutputType)type;
            }

            throw new ArgumentException(
                "The specified type is not an output type.",
                nameof(nativeType));
        }

        public IInputType CreateInputType(
            ITypeRegistry typeRegistry, Type nativeType)
        {
            TypeInfo typeInfo = GetOrCreateTypeInfo(nativeType);
            IType type = typeInfo.TypeFactory(typeRegistry);
            if (type.IsInputType())
            {
                return (IInputType)type;
            }

            throw new ArgumentException(
                "The specified type is not an input type.",
                nameof(nativeType));
        }

        private TypeInfo GetOrCreateTypeInfo(Type nativeType)
        {
            lock (_typeInfoCache)
            {
                if (!_typeInfoCache.TryGetValue(nativeType, out TypeInfo typeInfo))
                {
                    if (typeof(IType).IsAssignableFrom(nativeType))
                    {
                        typeInfo = CreateTypeInfoInternal(nativeType);
                        _typeInfoCache[nativeType] = typeInfo;
                    }
                }
                return typeInfo;
            }
        }

        private static TypeInfo CreateTypeInfoInternal(Type nativeType)
        {
            List<Type> types = DecomposeType(nativeType);

            TypeInfo typeInfo;
            if (!TryCreate4ComponentType(types, out typeInfo)
                && !TryCreate3ComponentType(types, out typeInfo)
                && !TryCreate2ComponentType(types, out typeInfo)
                && !TryCreate1ComponentType(types, out typeInfo))
            {
                throw new NotSupportedException(
                    "The specified type is not supported in this context.");
            }

            return typeInfo;
        }

        private static List<Type> DecomposeType(Type type)
        {
            List<Type> types = new List<Type>();
            Type current = type;

            do
            {
                types.Add(current);
                current = GetInnerType(current);
            } while (current != null && types.Count < 4);

            return types;
        }

        private static bool TryCreate4ComponentType(
            List<Type> types, out TypeInfo typeInfo)
        {
            if (types.Count == 4
                && IsNonNullType(types[0])
                && IsListType(types[1])
                && IsNonNullType(types[2])
                && IsNamedType(types[3]))
            {
                Func<ITypeRegistry, IType> factory = r =>
                    new NonNullType(new ListType(new NonNullType(
                        r.GetType<INamedType>(types[3]))));
                typeInfo = new TypeInfo(types[3], factory);
                return true;
            }

            typeInfo = default;
            return false;
        }

        private static bool TryCreate3ComponentType(
            List<Type> types, out TypeInfo typeInfo)
        {
            if (types.Count == 3)
            {
                if (IsListType(types[0])
                    && IsNonNullType(types[1])
                    && IsNamedType(types[2]))
                {
                    Func<ITypeRegistry, IType> factory = r =>
                        new ListType(new NonNullType(
                            r.GetType<INamedType>(types[2])));
                    typeInfo = new TypeInfo(types[2], factory);
                    return true;
                }

                if (IsNonNullType(types[0])
                    && IsListType(types[1])
                    && IsNamedType(types[2]))
                {
                    Func<ITypeRegistry, IType> factory = r =>
                        new NonNullType(new ListType(
                            r.GetType<INamedType>(types[2])));
                    typeInfo = new TypeInfo(types[2], factory);
                    return true;
                }
            }

            typeInfo = default;
            return false;
        }

        private static bool TryCreate2ComponentType(
            List<Type> types, out TypeInfo typeInfo)
        {
            if (types.Count == 2)
            {
                if (IsNonNullType(types[0])
                    && IsNamedType(types[1]))
                {
                    Func<ITypeRegistry, IType> factory = r =>
                        new NonNullType(
                            r.GetType<INamedType>(types[1]));
                    typeInfo = new TypeInfo(types[1], factory);
                    return true;
                }

                if (IsListType(types[0])
                    && IsNamedType(types[1]))
                {
                    Func<ITypeRegistry, IType> factory = r =>
                        new ListType(
                            r.GetType<INamedType>(types[1]));
                    typeInfo = new TypeInfo(types[1], factory);
                    return true;
                }
            }

            typeInfo = default;
            return false;
        }

        private static bool TryCreate1ComponentType(
            List<Type> types, out TypeInfo typeInfo)
        {
            if (types.Count == 1
               && IsNamedType(types[0]))
            {
                Func<ITypeRegistry, IType> factory = r =>
                    r.GetType<INamedType>(types[0]);
                typeInfo = new TypeInfo(types[0], factory);
                return true;
            }

            typeInfo = default;
            return false;
        }

        private static Type GetInnerType(Type type)
        {
            if (typeof(INamedType).IsAssignableFrom(type))
            {
                return null;
            }

            if (type.IsGenericType)
            {
                return type.GetGenericArguments().First();
            }

            return null;
        }

        private static bool IsListType(Type type)
        {
            return type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(ListType<>);
        }

        private static bool IsNonNullType(Type type)
        {
            return type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(NonNullType<>);
        }

        private static bool IsNamedType(Type type)
        {
            return typeof(INamedType).IsAssignableFrom(type);
        }

        internal static TypeInspector Default { get; } = new TypeInspector();
    }
}
