using System.Diagnostics.CodeAnalysis;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Configuration;

internal sealed class TypeRegistry
{
    private readonly Dictionary<TypeReference, RegisteredType> _typeRegister = [];
    private readonly Dictionary<ExtendedTypeReference, TypeReference> _runtimeTypeRefs =
        new(new ExtendedTypeRefEqualityComparer());
    private readonly HashSet<ExtendedTypeReference> _explicitRuntimeTypeRefs =
        new(new ExtendedTypeRefEqualityComparer());
    private readonly Dictionary<string, TypeReference> _nameRefs = new(StringComparer.Ordinal);
    private readonly Dictionary<FactoryTypeReference, TypeReference> _lookups = new(new TypeRefEqualityComparer());
    private readonly List<RegisteredType> _types = [];
    private readonly TypeInterceptor _typeRegistryInterceptor;

    public TypeRegistry(TypeInterceptor typeRegistryInterceptor)
    {
        ArgumentNullException.ThrowIfNull(typeRegistryInterceptor);

        _typeRegistryInterceptor = typeRegistryInterceptor;
    }

    public int Count => _typeRegister.Count;

    public IReadOnlyList<RegisteredType> Types => _types;

    public IReadOnlyDictionary<ExtendedTypeReference, TypeReference> RuntimeTypeRefs
        => _runtimeTypeRefs;

    public IReadOnlyDictionary<FactoryTypeReference, TypeReference> Lookups => _lookups;

    public IReadOnlyDictionary<string, TypeReference> NameRefs => _nameRefs;

    public bool IsRegistered(TypeReference typeReference)
    {
        ArgumentNullException.ThrowIfNull(typeReference);

        if (_typeRegister.ContainsKey(typeReference))
        {
            return true;
        }

        if (typeReference is ExtendedTypeReference extendedTypeRef
            && TryGetRuntimeTypeRefInternal(extendedTypeRef, out var reference))
        {
            return _typeRegister.ContainsKey(reference);
        }

        return false;
    }

    public bool TryGetType(
        TypeReference typeRef,
        [NotNullWhen(true)] out RegisteredType? registeredType)
    {
        ArgumentNullException.ThrowIfNull(typeRef);

        if (typeRef is ExtendedTypeReference clrTypeRef
            && TryGetRuntimeTypeRefInternal(clrTypeRef, out var internalRef))
        {
            typeRef = internalRef;
        }

        return _typeRegister.TryGetValue(typeRef, out registeredType);
    }

    public bool TryGetTypeRef(
        ExtendedTypeReference runtimeTypeRef,
        [NotNullWhen(true)] out TypeReference? typeRef)
    {
        ArgumentNullException.ThrowIfNull(runtimeTypeRef);

        return TryGetRuntimeTypeRefInternal(runtimeTypeRef, out typeRef);
    }

    public bool TryGetNonInferredTypeRef(
        ExtendedTypeReference runtimeTypeRef,
        [NotNullWhen(true)] out TypeReference? typeRef)
    {
        ArgumentNullException.ThrowIfNull(runtimeTypeRef);

        if (RuntimeTypeBindingHelper.RequiresExactBinding(runtimeTypeRef.Type)
            || !IsKeyValuePair(runtimeTypeRef.Type))
        {
            typeRef = null;
            return false;
        }

        foreach (var (candidateRef, candidateTypeRef) in _runtimeTypeRefs)
        {
            if (!candidateRef.Scope.EqualsOrdinal(runtimeTypeRef.Scope))
            {
                continue;
            }

            if (candidateRef.Context != runtimeTypeRef.Context
                && candidateRef.Context != TypeContext.None
                && runtimeTypeRef.Context != TypeContext.None)
            {
                continue;
            }

            if (candidateRef.Type.Type != runtimeTypeRef.Type.Type
                || candidateRef.Type.Kind != runtimeTypeRef.Type.Kind)
            {
                continue;
            }

            if (_typeRegister.TryGetValue(candidateTypeRef, out var registeredType)
                && !registeredType.IsInferred)
            {
                typeRef = candidateTypeRef;
                return true;
            }
        }

        typeRef = null;
        return false;
    }

    private static bool IsKeyValuePair(IExtendedType type)
        => type.IsGeneric && type.Definition == typeof(KeyValuePair<,>);

    public bool IsExplicitBinding(ExtendedTypeReference runtimeTypeRef)
    {
        ArgumentNullException.ThrowIfNull(runtimeTypeRef);

        if (_explicitRuntimeTypeRefs.Contains(runtimeTypeRef))
        {
            return true;
        }

        return runtimeTypeRef.Context is not TypeContext.None
            && _explicitRuntimeTypeRefs.Contains(runtimeTypeRef.WithContext());
    }

    public bool TryGetTypeRef(
        string typeName,
        [NotNullWhen(true)] out TypeReference? typeRef)
    {
        typeName.EnsureGraphQLName();

        if (!_nameRefs.TryGetValue(typeName, out typeRef))
        {
            typeRef = Types
                .FirstOrDefault(t => !t.IsExtension && t.Type.Name.EqualsOrdinal(typeName))
                ?.References[0];
        }
        return typeRef is not null;
    }

    public IEnumerable<TypeReference> GetTypeRefs() => _runtimeTypeRefs.Values;

    private bool TryGetRuntimeTypeRefInternal(
        ExtendedTypeReference runtimeTypeRef,
        [NotNullWhen(true)] out TypeReference? typeRef)
    {
        if (_runtimeTypeRefs.TryGetValue(runtimeTypeRef, out typeRef))
        {
            return true;
        }

        if (runtimeTypeRef.Context is not TypeContext.None)
        {
            return _runtimeTypeRefs.TryGetValue(runtimeTypeRef.WithContext(), out typeRef);
        }

        typeRef = null;
        return false;
    }

    public void TryRegister(
        ExtendedTypeReference runtimeTypeRef,
        TypeReference typeRef,
        bool explicitBinding = false)
    {
        ArgumentNullException.ThrowIfNull(runtimeTypeRef);
        ArgumentNullException.ThrowIfNull(typeRef);

        _runtimeTypeRefs.TryAdd(runtimeTypeRef, typeRef);

        if (explicitBinding)
        {
            _explicitRuntimeTypeRefs.Add(runtimeTypeRef);
        }
    }

    public void Register(RegisteredType registeredType)
    {
        ArgumentNullException.ThrowIfNull(registeredType);

        var addToTypes = !_typeRegister.ContainsValue(registeredType);

        foreach (var typeReference in registeredType.References)
        {
            if (_typeRegister.TryGetValue(typeReference, out var current)
                && !ReferenceEquals(current, registeredType))
            {
                if (current.IsInferred && !registeredType.IsInferred)
                {
                    _typeRegister[typeReference] = registeredType;
                    if (!_typeRegister.ContainsValue(current))
                    {
                        _types.Remove(current);
                    }
                }
            }
            else
            {
                _typeRegister[typeReference] = registeredType;
            }
        }

        if (addToTypes)
        {
            _types.Add(registeredType);
            _typeRegistryInterceptor.OnTypeRegistered(registeredType);
        }

        if (!registeredType.IsExtension)
        {
            if (registeredType.IsNamedType
                && registeredType.Type is ITypeConfigurationProvider { Configuration: { } typeDef }
                && !_nameRefs.ContainsKey(typeDef.Name))
            {
                _nameRefs.Add(typeDef.Name, registeredType.References[0]);
            }
            else if (registeredType.Kind == TypeKind.Scalar
                && registeredType.Type is ScalarType scalar
                && !_nameRefs.ContainsKey(scalar.Name))
            {
                _nameRefs.Add(scalar.Name, registeredType.References[0]);
            }
            else if (registeredType.Kind == TypeKind.Directive
                && registeredType.Type is DirectiveType directive
                && !_nameRefs.ContainsKey(directive.Configuration!.Name))
            {
                _nameRefs.Add(directive.Configuration.Name, registeredType.References[0]);
            }
        }
    }

    public void Register(string typeName, ExtendedTypeReference typeReference)
    {
        ArgumentNullException.ThrowIfNull(typeReference);

        typeName.EnsureGraphQLName();

        _nameRefs[typeName] = typeReference;
    }

    public void Register(string typeName, RegisteredType registeredType)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentNullException.ThrowIfNull(registeredType);

        if (registeredType.IsExtension)
        {
            return;
        }

        if (registeredType is { IsNamedType: false, IsDirectiveType: false })
        {
            return;
        }

        if (TryGetTypeRef(typeName, out var typeRef)
            && TryGetType(typeRef, out var type)
            && !ReferenceEquals(type, registeredType))
        {
            throw TypeInitializer_DuplicateTypeName(registeredType.Type, type.Type);
        }

        _nameRefs[typeName] = registeredType.References[0];
    }

    public void Register(FactoryTypeReference factoryRef, TypeReference typeDefRef)
    {
        ArgumentNullException.ThrowIfNull(factoryRef);
        ArgumentNullException.ThrowIfNull(typeDefRef);

        _lookups.TryAdd(factoryRef, typeDefRef);
    }

    public void CompleteDiscovery()
    {
        foreach (var registeredType in _types)
        {
            TypeReference reference = TypeReference.Create(registeredType.Type);
            registeredType.References.TryAdd(reference);
            _typeRegister[reference] = registeredType;

            if (registeredType.Type.Scope is { } s)
            {
                reference = TypeReference.Create(registeredType.Type, s);
                registeredType.References.TryAdd(reference);
                _typeRegister[reference] = registeredType;
            }
        }
    }
}
