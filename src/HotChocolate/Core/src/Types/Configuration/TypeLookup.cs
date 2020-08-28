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

    internal class TypeRegistry
    {
        private readonly Dictionary<ITypeReference, RegisteredType> _types =
            new Dictionary<ITypeReference, RegisteredType>();
        private readonly Dictionary<ExtendedTypeReference, ITypeReference> _runtimeTypeRefs =
            new Dictionary<ExtendedTypeReference, ITypeReference>(
                new ExtendedTypeReferenceEqualityComparer());
        private readonly Dictionary<NameString, ITypeReference> _nameRefs =
            new Dictionary<NameString, ITypeReference>();

        private int Count => _types.Count;

        public IReadOnlyList<RegisteredType> Types { get; private set; } =
            Array.Empty<RegisteredType>();

        public bool IsRegistered(ITypeReference typeReference)
        {
            if (_types.ContainsKey(typeReference))
            {
                return true;
            }

            if (typeReference is ExtendedTypeReference clrTypeReference)
            {
                return _runtimeTypeRefs.ContainsKey(clrTypeReference);
            }

            return false;
        }

        public bool TryGetType(
            ITypeReference typeRef,
            [NotNullWhen(true)] out RegisteredType? registeredType)
        {
            if (typeRef is ExtendedTypeReference clrTypeRef &&
                _runtimeTypeRefs.TryGetValue(clrTypeRef, out ITypeReference? internalRef))
            {
                typeRef = internalRef;
            }

            return _types.TryGetValue(typeRef, out registeredType);
        }

        public bool TryGetTypeRef(
            ExtendedTypeReference runtimeTypeRef,
            [NotNullWhen(true)] out ITypeReference? typeRef) =>
            _runtimeTypeRefs.TryGetValue(runtimeTypeRef, out typeRef);

        public bool TryGetType(
            NameString typeName,
            [NotNullWhen(true)] out RegisteredType? registeredType)
        {
            registeredType = null;
            return _nameRefs.TryGetValue(typeName, out ITypeReference? typeRef) &&
                _types.TryGetValue(typeRef, out registeredType);
        }

        public IEnumerable<ITypeReference> GetTypeRefs() => _runtimeTypeRefs.Values;

        public void TryRegister(ExtendedTypeReference runtimeTypeRef, ITypeReference typeRef)
        {
            if (!_runtimeTypeRefs.ContainsKey(runtimeTypeRef))
            {
                _runtimeTypeRefs.Add(runtimeTypeRef, typeRef);
            }
        }

        public void Register(RegisteredType registeredType)
        {
            foreach (ITypeReference typeReference in registeredType.References)
            {
                _types[typeReference] = registeredType;
            }
        }

        public void RebuildIndexes(bool names = false)
        {
            Types = new List<RegisteredType>(_types.Values.Distinct());

            if (names)
            {
                foreach (RegisteredType type in Types)
                {
                    _nameRefs[type.Type.Name] = type.References[0];
                }
            }
        }
    }
}
