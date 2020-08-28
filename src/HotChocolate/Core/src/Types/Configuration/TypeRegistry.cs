using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Types.Descriptors;

#nullable  enable

namespace HotChocolate.Configuration
{
    internal sealed class TypeRegistry
    {
        private readonly Dictionary<ITypeReference, RegisteredType> _types =
            new Dictionary<ITypeReference, RegisteredType>();
        private readonly Dictionary<ExtendedTypeReference, ITypeReference> _runtimeTypeRefs =
            new Dictionary<ExtendedTypeReference, ITypeReference>(
                new ExtendedTypeReferenceEqualityComparer());
        private readonly Dictionary<NameString, ITypeReference> _nameRefs =
            new Dictionary<NameString, ITypeReference>();

        public int Count => _types.Count;

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

        public bool TryGetTypeRef(
            NameString typeName,
            [NotNullWhen(true)] out ITypeReference? typeRef)
        {
            if (!_nameRefs.TryGetValue(typeName, out typeRef))
            {
                typeRef = Types
                    .FirstOrDefault(t => t.Type.Name.Equals(typeName))
                    ?.References[0];
                if (typeRef is not null)
                {
                    _nameRefs[typeName] = typeRef;
                }
            }
            return typeRef is not null;
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
