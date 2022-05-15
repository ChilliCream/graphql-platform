using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using HotChocolate.Configuration.Validation;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;

#nullable enable

namespace HotChocolate.Configuration;

internal class TypeInitializer
{
    private readonly List<FieldMiddleware> _globalComps = new();
    private readonly List<ISchemaError> _errors = new();
    private readonly IDescriptorContext _context;
    private readonly IReadOnlyList<ITypeReference> _initialTypes;
    private readonly TypeInterceptor _interceptor;
    private readonly IsOfTypeFallback? _isOfType;
    private readonly Func<TypeSystemObjectBase, RootTypeKind> _getTypeKind;
    private readonly IReadOnlySchemaOptions _options;
    private readonly TypeRegistry _typeRegistry;
    private readonly TypeLookup _typeLookup;
    private readonly TypeReferenceResolver _typeReferenceResolver;
    private readonly List<RegisteredType> _next = new();
    private readonly List<RegisteredType> _temp = new();
    private readonly List<ITypeReference> _typeRefs = new();
    private readonly HashSet<ITypeReference> _typeRefSet = new();

    public TypeInitializer(
        IDescriptorContext descriptorContext,
        TypeRegistry typeRegistry,
        IReadOnlyList<ITypeReference> initialTypes,
        IsOfTypeFallback? isOfType,
        Func<TypeSystemObjectBase, RootTypeKind> getTypeKind,
        IReadOnlySchemaOptions options)
    {
        _context = descriptorContext ??
            throw new ArgumentNullException(nameof(descriptorContext));
        _typeRegistry = typeRegistry ??
            throw new ArgumentNullException(nameof(typeRegistry));
        _initialTypes = initialTypes ??
            throw new ArgumentNullException(nameof(initialTypes));
        _getTypeKind = getTypeKind ??
            throw new ArgumentNullException(nameof(getTypeKind));
        _options = options ??
            throw new ArgumentNullException(nameof(options));

        _isOfType = isOfType ?? options.DefaultIsOfTypeCheck;

        _interceptor = descriptorContext.TypeInterceptor;
        ITypeInspector typeInspector = descriptorContext.TypeInspector;
        _typeLookup = new TypeLookup(typeInspector, _typeRegistry);
        _typeReferenceResolver = new TypeReferenceResolver(
            typeInspector, _typeRegistry, _typeLookup);

        _interceptor.InitializeContext(
            descriptorContext,
            this,
            _typeRegistry,
            _typeLookup,
            _typeReferenceResolver);
    }

    public IList<FieldMiddleware> GlobalComponents => _globalComps;

    public void Initialize()
    {
        // first we are going to find and initialize all types that belong to our schema.
        DiscoverTypes();

        // now that we have the resolvers sorted and know what types our schema will roughly
        // consist of we are going to have a look if we can infer interface usage
        // from .NET classes that implement .NET interfaces.
        RegisterImplicitInterfaceDependencies();

        // with all types (implicit and explicit) known we complete the type names.
        CompleteNames();

        // with the type names all known we can now build pairs to bring together types and
        // their type extensions.
        MergeTypeExtensions();

        // last we complete the types. Completing types means that we will assign all
        // the fields resolving all missing parts and then making the types immutable.
        CompleteTypes();

        // at this point everything is completely initialized and we just trigger a type
        // finalize to allow the type to cleanup any initialization data structures.
        FinalizeTypes();

        // if we do not have any errors we will validate the types for spec violations.
        if (_errors.Count == 0)
        {
            _errors.AddRange(SchemaValidator.Validate(
                _typeRegistry.Types.Select(t => t.Type),
                _options));
        }

        if (_errors.Count > 0)
        {
            throw new SchemaException(_errors);
        }
    }

    private void DiscoverTypes()
    {
        _interceptor.OnBeforeDiscoverTypes();

        var typeRegistrar = new TypeDiscoverer(
            _context,
            _typeRegistry,
            _typeLookup,
            _initialTypes,
            _interceptor);

        if (typeRegistrar.DiscoverTypes() is { Count: > 0 } errors)
        {
            throw new SchemaException(errors);
        }

        // lets tell the type interceptors what types we have initialized.
        if (_interceptor.TriggerAggregations)
        {
            _interceptor.OnTypesInitialized(_typeRegistry.Types);
        }

        _interceptor.OnAfterDiscoverTypes();
    }

    private void RegisterImplicitInterfaceDependencies()
    {
        var withRuntimeType = _typeRegistry.Types
            .Where(t => !t.IsIntrospectionType && t.RuntimeType != typeof(object))
            .Distinct()
            .ToList();

        var interfaceTypes = withRuntimeType
            .Where(t => t.Kind is TypeKind.Interface)
            .Distinct()
            .ToList();

        if (interfaceTypes.Count == 0)
        {
            return;
        }

        var objectTypes = withRuntimeType
            .Where(t => t.Kind is TypeKind.Object)
            .Distinct()
            .ToList();

        foreach (RegisteredType objectType in objectTypes)
        {
            foreach (RegisteredType interfaceType in interfaceTypes)
            {
                if (interfaceType.RuntimeType.IsAssignableFrom(objectType.RuntimeType))
                {
                    SchemaTypeReference typeReference = TypeReference.Create(interfaceType.Type);
                    ((ObjectType)objectType.Type).Definition!.Interfaces.Add(typeReference);
                    objectType.Dependencies.Add(new(typeReference, TypeDependencyKind.Completed));
                }
            }
        }

        foreach (RegisteredType implementing in interfaceTypes)
        {
            foreach (RegisteredType interfaceType in interfaceTypes)
            {
                if (!ReferenceEquals(implementing, interfaceType) &&
                    interfaceType.RuntimeType.IsAssignableFrom(implementing.RuntimeType))
                {
                    SchemaTypeReference typeReference = TypeReference.Create(interfaceType.Type);
                    ((InterfaceType)implementing.Type).Definition!.Interfaces.Add(typeReference);
                    implementing.Dependencies.Add(new(typeReference, TypeDependencyKind.Completed));
                }
            }
        }
    }

    private void CompleteNames()
    {
        _interceptor.OnBeforeCompleteTypeNames();

        if (ProcessTypes(TypeDependencyKind.Named, CompleteTypeName) &&
            _interceptor.TriggerAggregations)
        {
            _interceptor.OnTypesCompletedName(_typeRegistry.Types);
        }

        EnsureNoErrors();

        _interceptor.OnAfterCompleteTypeNames();
    }

    internal RegisteredType InitializeType(
        TypeSystemObjectBase type)
    {
        var typeReg = new RegisteredType(
            type,
            false,
            _typeRegistry,
            _typeLookup,
            _context,
            _interceptor,
            null);

        typeReg.References.Add(TypeReference.Create(type));

        _typeRegistry.Register(typeReg);
        typeReg.Type.Initialize(typeReg);

        return typeReg;
    }

    internal bool CompleteTypeName(RegisteredType registeredType)
    {
        registeredType.PrepareForCompletion(
            _typeReferenceResolver,
            _globalComps,
            _isOfType);

        registeredType.Type.CompleteName(registeredType);

        if (registeredType.IsNamedType || registeredType.IsDirectiveType)
        {
            _typeRegistry.Register(registeredType.Type.Name, registeredType);
        }

        RootTypeKind kind = _getTypeKind(registeredType.Type);
        registeredType.IsQueryType = kind == RootTypeKind.Query;
        registeredType.IsMutationType = kind == RootTypeKind.Mutation;
        registeredType.IsSubscriptionType = kind == RootTypeKind.Subscription;

        if (kind is not RootTypeKind.None)
        {
            OperationType operationType = kind switch
            {
                RootTypeKind.Query => OperationType.Query,
                RootTypeKind.Mutation => OperationType.Mutation,
                RootTypeKind.Subscription => OperationType.Subscription,
                _ => throw new NotSupportedException()
            };

            _interceptor.OnAfterResolveRootType(
                registeredType,
                ((ObjectType)registeredType.Type).Definition!,
                operationType,
                _context.ContextData);
        }

        return true;
    }

    private void MergeTypeExtensions()
    {
        _interceptor.OnBeforeMergeTypeExtensions();

        var extensions = _typeRegistry.Types
            .Where(t => t.IsExtension)
            .ToList();

        if (extensions.Count > 0)
        {
            var processed = new HashSet<RegisteredType>();

            var types = _typeRegistry.Types
                .Where(t => t.IsNamedType)
                .ToList();

            foreach (NameString typeName in extensions
                .Select(t => t.Type)
                .OfType<INamedTypeExtension>()
                .Where(t => t.ExtendsType is null)
                .Select(t => t.Name)
                .Distinct())
            {
                RegisteredType? type = types.FirstOrDefault(t => t.Type.Name.Equals(typeName));
                if (type?.Type is INamedType namedType)
                {
                    MergeTypeExtension(
                        extensions.Where(t => t.Type.Name.Equals(typeName)),
                        type,
                        namedType,
                        processed);
                }
            }

            var extensionArray = new RegisteredType[1];

            foreach (RegisteredType? extension in extensions.Except(processed))
            {
                if (extension.Type is INamedTypeExtension
                    {
                        ExtendsType: { } extendsType
                    } namedTypeExtension)
                {
                    var isSchemaType = typeof(INamedType).IsAssignableFrom(extendsType);
                    extensionArray[0] = extension;

                    foreach (RegisteredType? possibleMatchingType in types
                        .Where(t =>
                            t.Type is INamedType n &&
                            n.Kind == namedTypeExtension.Kind))

                    {
                        if (isSchemaType &&
                            extendsType.IsInstanceOfType(possibleMatchingType))
                        {
                            MergeTypeExtension(
                                extensionArray,
                                possibleMatchingType,
                                (INamedType)possibleMatchingType.Type,
                                processed);
                        }
                        else if (!isSchemaType &&
                            possibleMatchingType.RuntimeType != typeof(object) &&
                            extendsType.IsAssignableFrom(possibleMatchingType.RuntimeType))
                        {
                            MergeTypeExtension(
                                extensionArray,
                                possibleMatchingType,
                                (INamedType)possibleMatchingType.Type,
                                processed);
                        }
                    }
                }
            }
        }

        _interceptor.OnAfterMergeTypeExtensions();
    }

    private void MergeTypeExtension(
        IEnumerable<RegisteredType> extensions,
        RegisteredType registeredType,
        INamedType namedType,
        HashSet<RegisteredType> processed)
    {
        foreach (RegisteredType extension in extensions)
        {
            processed.Add(extension);

            if (extension.Type is INamedTypeExtensionMerger m)
            {
                if (m.Kind != namedType.Kind)
                {
                    throw new SchemaException(SchemaErrorBuilder.New()
                        .SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            TypeInitializer_Merge_KindDoesNotMatch,
                            namedType.Name))
                        .SetTypeSystemObject((ITypeSystemObject)namedType)
                        .Build());
                }

                // merge
                extension.Status = TypeStatus.Named;
                m.Merge(extension, namedType);

                // update dependencies
                registeredType.Dependencies.AddRange(extension.Dependencies);
                _typeRegistry.Register(registeredType);
            }
        }
    }

    private void CompleteTypes()
    {
        bool CompleteType(RegisteredType registeredType)
        {
            if (!registeredType.IsExtension)
            {
                registeredType.Status = TypeStatus.Named;
                registeredType.Type.CompleteType(registeredType);
            }
            return true;
        }

        _interceptor.OnBeforeCompleteTypes();

        ProcessTypes(TypeDependencyKind.Completed, CompleteType);
        EnsureNoErrors();

        if (_interceptor.TriggerAggregations)
        {
            _interceptor.OnTypesCompleted(_typeRegistry.Types);
        }

        _interceptor.OnAfterCompleteTypes();
    }

    private void FinalizeTypes()
    {
        foreach (RegisteredType? registeredType in _typeRegistry.Types)
        {
            if (!registeredType.IsExtension)
            {
                registeredType.Type.FinalizeType(registeredType);
            }
        }
    }

    private bool ProcessTypes(
        TypeDependencyKind kind,
        Func<RegisteredType, bool> action)
    {
        var processed = new HashSet<ITypeReference>();
        var batch = new List<RegisteredType>(GetInitialBatch(kind));
        var failed = false;

        while (!failed
            && processed.Count < _typeRegistry.Count
            && batch.Count > 0)
        {
            foreach (RegisteredType registeredType in batch)
            {
                if (!action(registeredType))
                {
                    failed = true;
                    break;
                }

                foreach (ITypeReference reference in registeredType.References)
                {
                    processed.Add(reference);
                }
            }

            if (!failed)
            {
                batch.Clear();
                batch.AddRange(GetNextBatch(processed));
            }
        }

        if (!failed && processed.Count < _typeRegistry.Count)
        {
            foreach (RegisteredType type in _typeRegistry.Types
                .Where(t => !processed.Contains(t.References[0])))
            {
                var name = type.Type.Name.HasValue
                    ? type.Type.Name.Value
                    : type.References[0].ToString()!;

                IReadOnlyList<ITypeReference> needed =
                    TryNormalizeDependencies(type.Conditionals,
                        out IReadOnlyList<ITypeReference>? normalized)
                        ? normalized.Except(processed).ToArray()
                        : type.Conditionals.Select(t => t.TypeReference).ToArray();

                _errors.Add(SchemaErrorBuilder.New()
                    .SetMessage(
                        TypeInitializer_CannotResolveDependency,
                        name,
                        string.Join(", ", needed))
                    .SetTypeSystemObject(type.Type)
                    .Build());
            }

            return false;
        }

        return _errors.Count == 0;
    }

    private IEnumerable<RegisteredType> GetInitialBatch(
        TypeDependencyKind kind)
    {
        _next.Clear();

        foreach (RegisteredType? registeredType in _typeRegistry.Types)
        {
            var conditional = false;
            registeredType.ClearConditionals();

            foreach (TypeDependency? dependency in registeredType.Dependencies)
            {
                if (dependency.Kind == kind)
                {
                    conditional = true;
                    registeredType.Conditionals.Add(dependency);
                }
            }

            if (conditional)
            {
                _next.Add(registeredType);
            }
            else
            {
                yield return registeredType;
            }
        }
    }

    private IEnumerable<RegisteredType> GetNextBatch(
        ISet<ITypeReference> processed)
    {
        foreach (RegisteredType type in _next)
        {
            if (TryNormalizeDependencies(type.Conditionals, out var normalized) &&
                processed.IsSupersetOf(GetTypeRefsExceptSelfRefs(type, normalized)))
            {
                yield return type;
            }
            else
            {
                _temp.Add(type);
            }
        }

        _next.Clear();

        if (_temp.Count > 0)
        {
            _next.AddRange(_temp);
            _temp.Clear();
        }

        List<ITypeReference> GetTypeRefsExceptSelfRefs(
            RegisteredType type,
            IReadOnlyList<ITypeReference> normalizedTypeReferences)
        {
            _typeRefs.Clear();
            _typeRefSet.Clear();
            _typeRefSet.UnionWith(type.References);

            foreach (ITypeReference? typeRef in normalizedTypeReferences)
            {
                if (_typeRefSet.Add(typeRef))
                {
                    _typeRefs.Add(typeRef);
                }
            }

            return _typeRefs;
        }
    }

    private bool TryNormalizeDependencies(
        List<TypeDependency> dependencies,
        [NotNullWhen(true)] out IReadOnlyList<ITypeReference>? normalized)
    {
        var n = new List<ITypeReference>();

        foreach (TypeDependency dependency in dependencies)
        {
            if (!_typeLookup.TryNormalizeReference(
                dependency.TypeReference,
                out ITypeReference? nr))
            {
                normalized = null;
                return false;
            }

            if (!n.Contains(nr))
            {
                n.Add(nr);
            }
        }

        normalized = n;
        return true;
    }

    private void EnsureNoErrors()
    {
        var errors = new List<ISchemaError>(_errors);

        foreach (RegisteredType type in _typeRegistry.Types)
        {
            if (type.Errors.Count == 0)
            {
                continue;
            }

            errors.AddRange(type.Errors);
        }

        if (errors.Count > 0)
        {
            throw new SchemaException(errors);
        }
    }
}
