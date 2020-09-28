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
    internal sealed class TypeLookup
    {
        private readonly Dictionary<ITypeReference, ITypeReference> _refs =
            new Dictionary<ITypeReference, ITypeReference>();
        private readonly ITypeInspector _typeInspector;
        private readonly TypeRegistry _typeRegistry;

        public TypeLookup(
            ITypeInspector typeInspector,
            TypeRegistry typeRegistry)
        {
            _typeInspector = typeInspector ??
                throw new ArgumentNullException(nameof(typeInspector));
            _typeRegistry = typeRegistry ??
                throw new ArgumentNullException(nameof(typeRegistry));
        }

        public bool TryNormalizeReference(
            ITypeReference typeRef,
            [NotNullWhen(true)] out ITypeReference? namedTypeRef)
        {
            if (typeRef is null)
            {
                throw new ArgumentNullException(nameof(typeRef));
            }

            // if we already created a lookup for this type reference we can just return the
            // the type reference to the named type.
            if (_refs.TryGetValue(typeRef, out namedTypeRef))
            {
                return true;
            }

            switch (typeRef)
            {
                case ExtendedTypeReference r:
                    if (TryNormalizeExtendedTypeReference(r, out namedTypeRef))
                    {
                        _refs[typeRef] = namedTypeRef;
                        return true;
                    }
                    break;

                case SchemaTypeReference r:
                    _refs[typeRef] = r;
                    namedTypeRef = r;
                    return true;

                case SyntaxTypeReference r:
                    NameString typeName = r.Type.NamedType().Name.Value;
                    if (_typeRegistry.TryGetTypeRef(typeName, out namedTypeRef))
                    {
                        _refs[typeRef] = namedTypeRef;
                        return true;
                    }
                    break;
            }

            namedTypeRef = null;
            return false;
        }

        public bool TryNormalizeReference(
            IDirectiveReference directiveRef,
            [NotNullWhen(true)] out ITypeReference? namedTypeRef)
        {
            if (directiveRef is null)
            {
                throw new ArgumentNullException(nameof(directiveRef));
            }

            if (directiveRef is ClrTypeDirectiveReference cr)
            {
                ExtendedTypeReference directiveTypeRef = _typeInspector.GetTypeRef(cr.ClrType);
                if (!_typeRegistry.TryGetTypeRef(directiveTypeRef, out namedTypeRef))
                {
                    namedTypeRef = directiveTypeRef;
                }
                return true;
            }

            if (directiveRef is NameDirectiveReference nr)
            {
                namedTypeRef = _typeRegistry.Types
                    .FirstOrDefault(t =>t.Type is DirectiveType && t.Type.Name.Equals(nr.Name))?
                    .References[0];
                return namedTypeRef is not null;
            }

            namedTypeRef = null;
            return false;
        }

        private bool TryNormalizeExtendedTypeReference(
            ExtendedTypeReference typeRef,
            [NotNullWhen(true)] out ITypeReference? namedTypeRef)
        {
            if (typeRef is null)
            {
                throw new ArgumentNullException(nameof(typeRef));
            }

            // if the typeRef refers to a schema type base class we skip since such a type is not
            // resolvable.
            if (typeRef.Type.Type.IsNonGenericSchemaType() ||
                !_typeInspector.TryCreateTypeInfo(typeRef.Type, out ITypeInfo? typeInfo))
            {
                namedTypeRef = null;
                return false;
            }

            // if we have a concrete schema type we will extract the named type component of
            // the type and rewrite the type reference.
            if (typeRef.Type.IsSchemaType)
            {
                namedTypeRef = typeRef.With(_typeInspector.GetType(typeInfo.NamedType));
                return true;
            }

            // we check each component layer since there could be a binding on a list type,
            // eg list<byte> to ByteArray.
            for (var i = 0; i < typeInfo.Components.Count; i++)
            {
                IExtendedType componentType = typeInfo.Components[i].Type;
                ExtendedTypeReference componentRef = typeRef.WithType(componentType);
                if (_typeRegistry.TryGetTypeRef(componentRef, out namedTypeRef) ||
                    _typeRegistry.TryGetTypeRef(componentRef.WithContext(), out namedTypeRef))
                {
                    return true;
                }
            }

            namedTypeRef = null;
            return false;
        }
    }
}
