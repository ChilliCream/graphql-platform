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
        private readonly ITypeInspector _typeInspector;
        private readonly IDictionary<ITypeReference, ITypeReference> _refs;
        private readonly IDictionary<ExtendedTypeReference, ITypeReference> _runtimeTypeRefs;
        private readonly IDictionary<NameString, ITypeReference> _nameTypeRefs;
        private readonly DiscoveredTypes _types;

        public TypeLookup(
            ITypeInspector typeInspector,
            IDictionary<ITypeReference, ITypeReference> refs,
            IDictionary<ExtendedTypeReference, ITypeReference> runtimeTypeRefs,
            IDictionary<NameString, ITypeReference> nameTypeRefs,
            DiscoveredTypes types)
        {
            _typeInspector = typeInspector;
            _refs = refs;
            _runtimeTypeRefs = runtimeTypeRefs;
            _nameTypeRefs = nameTypeRefs;
            _types = types;
        }

        public bool TryNormalizeReference(
            ITypeReference typeRef,
            [NotNullWhen(true)] out ITypeReference? namedTypeRef)
        {
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
                    namedTypeRef = _typeInspector.GetTypeRef(
                        r.Type.GetType(), r.Context, typeRef.Scope);
                    _refs[typeRef] = namedTypeRef;
                    return true;

                case SyntaxTypeReference r:
                    NameString typeName = r.Type.NamedType().Name.Value;
                    if (_nameTypeRefs.TryGetValue(typeName, out namedTypeRef))
                    {
                        _refs[typeRef] = namedTypeRef;
                        return true;
                    }

                    namedTypeRef = _types.Types
                        .FirstOrDefault(t => t.Type.Name.Equals(typeName))
                        ?.References[0];

                    if (namedTypeRef is not null)
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
            if (directiveRef is ClrTypeDirectiveReference cr)
            {
                ExtendedTypeReference directiveTypeRef = _typeInspector.GetTypeRef(cr.ClrType);
                if (!_runtimeTypeRefs.TryGetValue(directiveTypeRef, out namedTypeRef))
                {
                    namedTypeRef = directiveTypeRef;
                }
                return namedTypeRef is not null;
            }

            if (directiveRef is NameDirectiveReference nr)
            {
                namedTypeRef = _types.Types
                    .FirstOrDefault(t => t.Type.Name.Equals(nr.Name) && t.Type is DirectiveType)?
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
                if (_runtimeTypeRefs.TryGetValue(componentRef, out namedTypeRef) ||
                    _runtimeTypeRefs.TryGetValue(componentRef.WithContext(), out namedTypeRef))
                {
                    return true;
                }
            }

            namedTypeRef = null;
            return false;
        }
    }
}
