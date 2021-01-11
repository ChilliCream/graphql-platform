using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable  enable

namespace HotChocolate.Configuration
{
    internal sealed class TypeReferenceResolver
    {
        private readonly Dictionary<TypeId, IType> _typeCache = new();
        private readonly ITypeInspector _typeInspector;
        private readonly TypeRegistry _typeRegistry;
        private readonly TypeLookup _typeLookup;

        public TypeReferenceResolver(
            ITypeInspector typeInspector,
            TypeRegistry typeRegistry,
            TypeLookup typeLookup)
        {
            _typeInspector = typeInspector ??
                throw new ArgumentNullException(nameof(typeInspector));
            _typeRegistry = typeRegistry ??
                throw new ArgumentNullException(nameof(typeRegistry));
            _typeLookup = typeLookup ??
                throw new ArgumentNullException(nameof(typeLookup));
        }

        public IEnumerable<T> GetTypes<T>() =>
            _typeRegistry.Types
                .Select(t => t.Type)
                .OfType<T>()
                .Distinct();

        public bool TryGetType(ITypeReference typeRef, [NotNullWhen(true)] out IType? type)
        {
            if (typeRef is null)
            {
                throw new ArgumentNullException(nameof(typeRef));
            }

            if (!_typeLookup.TryNormalizeReference(typeRef, out ITypeReference? namedTypeRef))
            {
                type = null;
                return false;
            }

            TypeId typeId = CreateId(typeRef, namedTypeRef);
            if (_typeCache.TryGetValue(typeId, out type))
            {
                return true;
            }

            if (namedTypeRef is SchemaTypeReference { Type: IType schemaType })
            {
                type = schemaType;
                _typeCache[typeId] = type;
                return true;
            }

            if (!_typeRegistry.TryGetType(namedTypeRef, out RegisteredType? registeredType) ||
                registeredType.Type is not INamedType)
            {
                type = null;
                return false;
            }

            var namedType = (INamedType)registeredType.Type;

            switch (typeRef)
            {
                case ExtendedTypeReference r:
                    ITypeFactory typeFactory = _typeInspector.CreateTypeFactory(r.Type);
                    type = typeFactory.CreateType(namedType);
                    _typeCache[typeId] = type;
                    return true;

                case SyntaxTypeReference r:
                    type = CreateType(namedType, r.Type);
                    return true;

                default:
                    throw new NotSupportedException();
            }
        }

        public bool TryGetDirectiveType(
            IDirectiveReference typeRef,
            [NotNullWhen(true)] out DirectiveType? directiveType)
        {
            if (typeRef is null)
            {
                throw new ArgumentNullException(nameof(typeRef));
            }

            if (!_typeLookup.TryNormalizeReference(typeRef, out ITypeReference? namedTypeRef))
            {
                directiveType = null;
                return false;
            }

            if (_typeRegistry.TryGetType(namedTypeRef, out RegisteredType? registeredType) &&
                registeredType.Type is DirectiveType d)
            {
                directiveType = d;
                return true;
            }

            directiveType = null;
            return false;
        }

        private static IType CreateType(
            IType namedType,
            ITypeNode typeNode)
        {
            if (typeNode is NonNullTypeNode nonNullType)
            {
                return new NonNullType(CreateType(namedType, nonNullType.Type));
            }

            if (typeNode is ListTypeNode listType)
            {
                return new ListType(CreateType(namedType, listType.Type));
            }

            return namedType;
        }

        private TypeId CreateId(ITypeReference typeRef, ITypeReference namedTypeRef)
        {
            switch (typeRef)
            {
                case ExtendedTypeReference r:
                    ITypeInfo typeInfo = _typeInspector.CreateTypeInfo(r.Type);
                    return new TypeId(namedTypeRef, CreateFlags(typeInfo));

                case SyntaxTypeReference r:
                    return new TypeId(namedTypeRef, CreateFlags(r.Type));

                case SchemaTypeReference:
                    return new TypeId(namedTypeRef, 1);

                default:
                    throw new NotSupportedException();
            }
        }

        private static int CreateFlags(ITypeInfo typeInfo)
        {
            var flags = 1;

            for (var i = 0; i < typeInfo.Components.Count; i++)
            {
                switch (typeInfo.Components[i].Kind)
                {
                    case TypeComponentKind.List:
                        flags <<= 1;
                        flags = flags | 1;
                        break;

                    case TypeComponentKind.NonNull:
                        flags <<= 1;
                        break;
                }
            }

            return flags;
        }

        private static int CreateFlags(ITypeNode type)
        {
            var flags = 1;
            ITypeNode current = type;

            while (current is not NamedTypeNode)
            {
                if (current is ListTypeNode listType)
                {
                    flags <<= 1;
                    flags = flags | 1;
                    current = listType.Type;
                }
                else if (current is NonNullTypeNode nonNullType)
                {
                    flags <<= 1;
                    current = nonNullType.Type;
                }
                else
                {
                    break;
                }
            }

            return flags;
        }

        private readonly struct TypeId : IEquatable<TypeId>
        {
            public TypeId(ITypeReference typeRef, int flags)
            {
                TypeRef = typeRef;
                Flags = flags;
            }

            public ITypeReference TypeRef { get; }

            public int Flags { get; }

            public bool Equals(TypeId other) =>
                TypeRef.Equals(other.TypeRef) && Flags == other.Flags;

            public override bool Equals(object? obj) =>
                obj is TypeId other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return TypeRef.GetHashCode() * 397 ^
                           Flags.GetHashCode() * 397;
                }
            }

            public static bool operator ==(TypeId left, TypeId right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(TypeId left, TypeId right)
            {
                return !left.Equals(right);
            }
        }
    }
}
