using System.Diagnostics.CodeAnalysis;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal sealed class TypeLookup
{
    private readonly Dictionary<TypeReference, NormalizedTypeReference> _refs = [];
    private readonly ITypeInspector _typeInspector;
    private readonly TypeRegistry _typeRegistry;

    public TypeLookup(
        ITypeInspector typeInspector,
        TypeRegistry typeRegistry)
    {
        ArgumentNullException.ThrowIfNull(typeInspector);
        ArgumentNullException.ThrowIfNull(typeRegistry);

        _typeInspector = typeInspector;
        _typeRegistry = typeRegistry;
    }

    public bool TryNormalizeReference(
        TypeReference typeRef,
        [NotNullWhen(true)] out TypeReference? namedTypeRef)
        => TryNormalizeReference(typeRef, out namedTypeRef, out _);

    /// <summary>
    /// Resolves <paramref name="typeRef"/> to the reference of its named type.
    /// For an <see cref="ExtendedTypeReference"/>, <paramref name="componentIndex"/> is the
    /// index of the type component that resolved to the named type; for any other reference
    /// it is -1.
    /// </summary>
    public bool TryNormalizeReference(
        TypeReference typeRef,
        [NotNullWhen(true)] out TypeReference? namedTypeRef,
        out int componentIndex)
    {
        ArgumentNullException.ThrowIfNull(typeRef);

        // if we already created a lookup for this type reference we can just return
        // the type reference to the named type.
        if (_refs.TryGetValue(typeRef, out var normalized))
        {
            namedTypeRef = normalized.NamedTypeRef;
            componentIndex = normalized.ComponentIndex;
            return true;
        }

        switch (typeRef)
        {
            case ExtendedTypeReference r:
                if (TryNormalizeExtendedTypeReference(r, out namedTypeRef, out componentIndex))
                {
                    _refs[typeRef] = new NormalizedTypeReference(namedTypeRef, componentIndex);
                    return true;
                }
                break;

            case SchemaTypeReference r:
                _refs[typeRef] = new NormalizedTypeReference(r, -1);
                namedTypeRef = r;
                componentIndex = -1;
                return true;

            case SyntaxTypeReference r:
                var typeName = r.Type.NamedType().Name.Value;
                if (_typeRegistry.TryGetTypeRef(typeName, out namedTypeRef))
                {
                    _refs[typeRef] = new NormalizedTypeReference(namedTypeRef, -1);
                    componentIndex = -1;
                    return true;
                }
                break;

            case DependantFactoryTypeReference r:
                _refs[typeRef] = new NormalizedTypeReference(r, -1);
                namedTypeRef = r;
                componentIndex = -1;
                return true;

            case NameDirectiveReference dirRef:
                if (_typeRegistry.TryGetTypeRef(dirRef.Name, out namedTypeRef))
                {
                    _refs[typeRef] = new NormalizedTypeReference(namedTypeRef, -1);
                    componentIndex = -1;
                    return true;
                }
                break;

            case ExtendedTypeDirectiveReference dirRef:
                if (TryNormalizeExtendedTypeReference(
                    TypeReference.Create(dirRef.Type),
                    out namedTypeRef,
                    out _))
                {
                    _refs[typeRef] = new NormalizedTypeReference(namedTypeRef, -1);
                    componentIndex = -1;
                    return true;
                }
                break;

            case FactoryTypeReference factoryRef:
                if (TryNormalizeReference(
                    factoryRef.TypeDefinition,
                    out namedTypeRef,
                    out componentIndex))
                {
                    return true;
                }
                break;
        }

        namedTypeRef = null;
        componentIndex = -1;
        return false;
    }

    private bool TryNormalizeExtendedTypeReference(
        ExtendedTypeReference typeRef,
        [NotNullWhen(true)] out TypeReference? namedTypeRef,
        out int componentIndex)
    {
        ArgumentNullException.ThrowIfNull(typeRef);

        // if the typeRef refers to a schema type base class we skip since such a type is not
        // resolvable.
        if (typeRef.Type.Type.IsNonGenericSchemaType()
            || !_typeInspector.TryCreateTypeInfo(typeRef.Type, out var typeInfo))
        {
            namedTypeRef = null;
            componentIndex = -1;
            return false;
        }

        // if we have a concrete schema type we will extract the named type component of
        // the type and rewrite the type reference.
        if (typeRef.Type.IsSchemaType)
        {
            namedTypeRef = typeRef.With(_typeInspector.GetType(typeInfo.NamedType));
            componentIndex = typeInfo.Components.Count - 1;
            return true;
        }

        // we check each component layer since there could be a binding on a list type,
        // e.g. list<byte> to Base64String.
        for (var i = 0; i < typeInfo.Components.Count; i++)
        {
            var componentType = typeInfo.Components[i].Type;
            var componentRef = typeRef.WithType(componentType);
            if (_typeRegistry.TryGetTypeRef(componentRef, out namedTypeRef)
                || _typeRegistry.TryGetTypeRef(componentRef.WithContext(), out namedTypeRef))
            {
                componentIndex = i;
                return true;
            }
        }

        namedTypeRef = null;
        componentIndex = -1;
        return false;
    }

    private readonly record struct NormalizedTypeReference(
        TypeReference NamedTypeRef,
        int ComponentIndex);
}
