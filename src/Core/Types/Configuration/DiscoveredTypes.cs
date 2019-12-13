using System.Security.AccessControl;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration
{
    internal sealed class DiscoveredTypes
    {
        private readonly IDictionary<ITypeReference, RegisteredType> _registered;
        private readonly IDictionary<IClrTypeReference, ITypeReference> _clrTypeReferences;

        public DiscoveredTypes(
            IDictionary<ITypeReference, RegisteredType> registered,
            IDictionary<IClrTypeReference, ITypeReference> clrTypeReferences,
            IReadOnlyCollection<ISchemaError> errors)
        {
            _registered = registered;
            _clrTypeReferences = clrTypeReferences;
            Errors = errors;
            Types = new HashSet<RegisteredType>(registered.Values);
        }

        public int TypeReferenceCount => _registered.Count;

        public ISet<RegisteredType> Types { get; private set; }

        public IReadOnlyCollection<ISchemaError> Errors { get; }

        public bool TryGetType(
            ITypeReference typeReference,
            out RegisteredType registeredType)
        {
            ITypeReference typeRef = typeReference;

            if (typeRef is IClrTypeReference clrTypeRef
                && _clrTypeReferences.TryGetValue(clrTypeRef, out ITypeReference t))
            {
                typeRef = t;
            }

            return _registered.TryGetValue(typeRef, out registeredType);
        }

        public void UpdateType(RegisteredType registeredType)
        {
            foreach (ITypeReference typeReference in registeredType.References)
            {
                _registered[typeReference] = registeredType;
            }
        }

        public void RebuildTypeSet()
        {
            Types = new HashSet<RegisteredType>(_registered.Values);
        }
    }
}
