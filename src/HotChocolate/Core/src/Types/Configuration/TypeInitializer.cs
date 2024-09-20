using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Types.Descriptors.Definitions.TypeDependencyFulfilled;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed class TypeInitializer
{
    private readonly List<FieldMiddleware> _globalComps = [];
    private readonly List<ISchemaError> _errors = [];
    private readonly IDescriptorContext _context;
    private readonly TypeInterceptor _interceptor;
    private readonly IsOfTypeFallback? _isOfType;
    private readonly Func<TypeSystemObjectBase, RootTypeKind> _getTypeKind;
    private readonly TypeRegistry _typeRegistry;
    private readonly TypeLookup _typeLookup;
    private readonly TypeReferenceResolver _typeReferenceResolver;
    private readonly List<RegisteredType> _next = [];
    private readonly List<RegisteredType> _temp = [];
    private readonly List<TypeReference> _typeRefs = [];
    private readonly HashSet<TypeReference> _typeRefSet = [];
    private readonly List<RegisteredRootType> _rootTypes = [];
    private readonly TypeDiscoverer _typeDiscoverer;

    public TypeInitializer(
        IDescriptorContext descriptorContext,
        TypeRegistry typeRegistry,
        IReadOnlyList<TypeReference> initialTypes,
        IsOfTypeFallback? isOfType,
        Func<TypeSystemObjectBase, RootTypeKind> getTypeKind,
        IReadOnlySchemaOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _context = descriptorContext ?? throw new ArgumentNullException(nameof(descriptorContext));
        _typeRegistry = typeRegistry ?? throw new ArgumentNullException(nameof(typeRegistry));
        var initialTypes1 = initialTypes ?? throw new ArgumentNullException(nameof(initialTypes));
        _getTypeKind = getTypeKind ?? throw new ArgumentNullException(nameof(getTypeKind));

        _isOfType = isOfType ?? options.DefaultIsOfTypeCheck;

        _interceptor = descriptorContext.TypeInterceptor;
        var typeInspector = descriptorContext.TypeInspector;
        _typeLookup = new TypeLookup(typeInspector, _typeRegistry);
        _typeReferenceResolver = new TypeReferenceResolver(
            typeInspector,
            _typeRegistry,
            _typeLookup);

        _interceptor.InitializeContext(
            descriptorContext,
            this,
            _typeRegistry,
            _typeLookup,
            _typeReferenceResolver);

        _typeDiscoverer = new TypeDiscoverer(
            _context,
            _typeRegistry,
            _typeLookup,
            initialTypes1,
            _interceptor);
    }

    public IList<FieldMiddleware> GlobalComponents => _globalComps;

    public void Initialize()
    {
        // first we are going to find and initialize all types that belong to our schema.
        DiscoverTypes();

        // now that we have the resolvers sorted and know what types our schema will roughly
        // consist of we are going to have a look if we can infer interface and union usage
        // from .NET classes that implement .NET interfaces.
        RegisterImplicitAbstractTypeDependencies();

        // with all types (implicit and explicit) known we complete the type names.
        CompleteNames();

        // with the type names all known we will announce the root type objects.
        ResolveRootTyped();

        // we can now build pairs to bring together types and their type extensions.
        MergeTypeExtensions();

        // before we start completing types we will compile the resolvers.
        CompileResolvers();

        // last we complete the types. Completing types means that we will assign all
        // the fields resolving all missing parts and then making the types immutable.
        CompleteTypes();

        // at this point everything is completely initialized, and we just trigger a type
        // finalize to allow the type to clean up any initialization data structures.
        FinalizeTypes();

        // if we do not have any errors we will validate the types for spec violations.
        if (_errors.Count > 0)
        {
            throw new SchemaException(_errors);
        }
    }

    private void DiscoverTypes()
    {
        _interceptor.OnBeforeDiscoverTypes();

        if (_typeDiscoverer.DiscoverTypes() is { Count: > 0, } errors)
        {
            throw new SchemaException(errors);
        }

        // lets tell the type interceptors what types we have initialized.
        _interceptor.OnTypesInitialized();
        _interceptor.OnAfterDiscoverTypes();
    }

    private void RegisterImplicitAbstractTypeDependencies()
    {
        var processed = new HashSet<RegisteredType>();
        var interfaceTypes = GetTypesWithRuntimeType(processed, TypeKind.Interface);
        var unionTypes = GetTypesWithRuntimeType(processed, TypeKind.Union);
        List<RegisteredType>? objectTypes = null;

        if (interfaceTypes.Count > 0)
        {
            objectTypes = GetTypesWithRuntimeType(processed, TypeKind.Object);

            foreach (var objectType in objectTypes)
            {
                foreach (var interfaceType in interfaceTypes)
                {
                    if (interfaceType.RuntimeType.IsAssignableFrom(objectType.RuntimeType))
                    {
                        var typeRef = interfaceType.TypeReference;
                        ((ObjectType)objectType.Type).Definition!.Interfaces.Add(typeRef);
                        objectType.Dependencies.Add(new(typeRef, Completed));
                    }
                }
            }

            foreach (var implementing in interfaceTypes)
            {
                foreach (var interfaceType in interfaceTypes)
                {
                    if (!ReferenceEquals(implementing, interfaceType)
                        && interfaceType.RuntimeType.IsAssignableFrom(implementing.RuntimeType))
                    {
                        var typeRef = interfaceType.TypeReference;
                        ((InterfaceType)implementing.Type).Definition!.Interfaces.Add(typeRef);
                        implementing.Dependencies.Add(new(typeRef, Completed));
                    }
                }
            }
        }

        if (unionTypes.Count > 0)
        {
            objectTypes ??= GetTypesWithRuntimeType(processed, TypeKind.Object);

            foreach (var objectType in objectTypes)
            {
                foreach (var unionType in unionTypes)
                {
                    if (unionType.RuntimeType.IsAssignableFrom(objectType.RuntimeType))
                    {
                        var typeRef = objectType.TypeReference;
                        ((UnionType)unionType.Type).Definition!.Types.Add(typeRef);
                    }
                }
            }
        }
    }

    private List<RegisteredType> GetTypesWithRuntimeType(
        HashSet<RegisteredType> processed,
        TypeKind kind)
    {
        var interfaces = new List<RegisteredType>();

        for (var i = 0; i < _typeRegistry.Types.Count; i++)
        {
            var type = _typeRegistry.Types[i];

            if (type.Kind == kind
                && processed.Add(type)
                && !type.IsIntrospectionType
                && type.RuntimeType != typeof(object))
            {
                interfaces.Add(type);
            }
        }

        return interfaces;
    }

    private void CompleteNames()
    {
        _interceptor.OnBeforeCompleteTypeNames();

        if (ProcessTypes(Named, type => CompleteTypeName(type)))
        {
            _interceptor.OnTypesCompletedName();
        }

        EnsureNoErrors();

        _interceptor.OnAfterCompleteTypeNames();
    }

    internal RegisteredType InitializeType(Type type)
    {
        var typeObj = _typeDiscoverer.Registrar.CreateInstance(type);
        return InitializeType(typeObj);
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
        registeredType.Status = TypeStatus.Named;

        if (registeredType.IsNamedType || registeredType.IsDirectiveType)
        {
            _typeRegistry.Register(registeredType.Type.Name, registeredType);
        }

        var kind = _getTypeKind(registeredType.Type);
        registeredType.IsQueryType = kind == RootTypeKind.Query;
        registeredType.IsMutationType = kind == RootTypeKind.Mutation;
        registeredType.IsSubscriptionType = kind == RootTypeKind.Subscription;

        if (kind is not RootTypeKind.None)
        {
            _rootTypes.Add(
                new RegisteredRootType(
                    registeredType,
                    registeredType,
                    (OperationType)(int)kind));
        }

        return true;
    }

    private void ResolveRootTyped()
    {
        foreach (var type in _rootTypes)
        {
            _interceptor.OnAfterResolveRootType(
                type.Context,
                ((ObjectType)type.Type.Type).Definition!,
                type.Kind);
        }
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

            foreach (var typeName in extensions
                .Select(t => t.Type)
                .OfType<INamedTypeExtension>()
                .Where(t => t.ExtendsType is null)
                .Select(t => t.Name)
                .Distinct())
            {
                var type = types.Find(t => t.Type.Name.EqualsOrdinal(typeName));

                if (type?.Type is INamedType namedType)
                {
                    MergeTypeExtension(
                        extensions.Where(t => t.Type.Name.EqualsOrdinal(typeName)),
                        type,
                        namedType,
                        processed);
                }
            }

            var extensionArray = new RegisteredType[1];

            foreach (var extension in extensions.Except(processed))
            {
                if (extension.Type is INamedTypeExtension
                    {
                        ExtendsType: { } extendsType,
                    } namedTypeExtension)
                {
                    var isSchemaType = typeof(INamedType).IsAssignableFrom(extendsType);
                    extensionArray[0] = extension;

                    foreach (var possibleMatchingType in types
                        .Where(
                            t =>
                                t.Type is INamedType n && n.Kind == namedTypeExtension.Kind))

                    {
                        if (isSchemaType && extendsType.IsInstanceOfType(possibleMatchingType.Type))
                        {
                            MergeTypeExtension(
                                extensionArray,
                                possibleMatchingType,
                                (INamedType)possibleMatchingType.Type,
                                processed);
                        }
                        else if (!isSchemaType
                            && possibleMatchingType.RuntimeType != typeof(object)
                            && extendsType.IsAssignableFrom(possibleMatchingType.RuntimeType))
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

        var mutationType = _rootTypes.FirstOrDefault(t => t.Kind == OperationType.Mutation);

        if (mutationType.IsInitialized)
        {
            _interceptor.OnBeforeCompleteMutation(
                mutationType.Type,
                ((ObjectType)mutationType.Type.Type).Definition!);
        }
    }

    private void MergeTypeExtension(
        IEnumerable<RegisteredType> extensions,
        RegisteredType registeredType,
        INamedType namedType,
        HashSet<RegisteredType> processed)
    {
        foreach (var extension in extensions)
        {
            processed.Add(extension);

            if (extension.Type is INamedTypeExtensionMerger m)
            {
                if (m.Kind != namedType.Kind)
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage(
                                string.Format(
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

    private void CompileResolvers()
    {
        foreach (var registeredType in _typeRegistry.Types)
        {
            CompileResolvers(registeredType);
        }
    }

    internal void CompileResolvers(RegisteredType registeredType)
    {
        if (registeredType.Type is ObjectType objectType)
        {
            foreach (var field in objectType.Definition!.Fields)
            {
                if (!field.Resolvers.HasResolvers)
                {
                    field.Resolvers = CompileResolver(field, _context.ResolverCompiler);
                }
            }
        }
        else if(registeredType.Type is InterfaceType interfaceType)
        {
            foreach (var field in interfaceType.Definition!.Fields)
            {
                if (!field.Resolvers.HasResolvers)
                {
                    field.Resolvers = CompileResolver(field, _context.ResolverCompiler);
                }
            }
        }
    }

    private static FieldResolverDelegates CompileResolver(
        ObjectFieldDefinition definition,
        IResolverCompiler resolverCompiler)
    {
        var resolvers = definition.Resolvers;

        if (resolvers.HasResolvers)
        {
            return resolvers;
        }

        if (definition.Expression is LambdaExpression lambdaExpression)
        {
            resolvers = resolverCompiler.CompileResolve(
                lambdaExpression,
                definition.SourceType
                ?? definition.Member?.ReflectedType ?? definition.Member?.DeclaringType ?? typeof(object),
                definition.ResolverType);
        }
        else if (definition.ResolverMember is not null)
        {
            var map = TypeMemHelper.RentArgumentNameMap();
            BuildArgumentLookup(definition, map);

            resolvers = resolverCompiler.CompileResolve(
                definition.ResolverMember,
                definition.SourceType
                ?? definition.Member?.ReflectedType ?? definition.Member?.DeclaringType ?? typeof(object),
                definition.ResolverType,
                map,
                definition.GetParameterExpressionBuilders());

            TypeMemHelper.Return(map);
        }
        else if (definition.Member is not null)
        {
            var map = TypeMemHelper.RentArgumentNameMap();
            BuildArgumentLookup(definition, map);

            resolvers = resolverCompiler.CompileResolve(
                definition.Member,
                definition.SourceType ?? definition.Member.ReflectedType ?? definition.Member.DeclaringType,
                definition.ResolverType,
                map,
                definition.GetParameterExpressionBuilders());

            TypeMemHelper.Return(map);
        }

        return resolvers;

        static void BuildArgumentLookup(
            ObjectFieldDefinition definition,
            Dictionary<ParameterInfo, string> argumentNames)
        {
            foreach (var argument in definition.Arguments)
            {
                if (argument.Parameter is not null)
                {
                    argumentNames[argument.Parameter] = argument.Name;
                }
            }
        }
    }

    private static FieldResolverDelegates CompileResolver(
        InterfaceFieldDefinition definition,
        IResolverCompiler resolverCompiler)
    {
        var resolvers = definition.Resolvers;

        if (resolvers.HasResolvers)
        {
            return resolvers;
        }

        if (definition.ResolverMember is not null)
        {
            var map = TypeMemHelper.RentArgumentNameMap();
            BuildArgumentLookup(definition, map);

            resolvers = resolverCompiler.CompileResolve(
                definition.ResolverMember,
                definition.SourceType
                ?? definition.Member?.ReflectedType ?? definition.Member?.DeclaringType ?? typeof(object),
                definition.ResolverType,
                map,
                definition.GetParameterExpressionBuilders());

            TypeMemHelper.Return(map);
        }
        else if (definition.Member is not null)
        {
            var map = TypeMemHelper.RentArgumentNameMap();
            BuildArgumentLookup(definition, map);

            resolvers = resolverCompiler.CompileResolve(
                definition.Member,
                definition.SourceType ?? definition.Member.ReflectedType ?? definition.Member.DeclaringType,
                definition.ResolverType,
                map,
                definition.GetParameterExpressionBuilders());

            TypeMemHelper.Return(map);
        }

        return resolvers;

        static void BuildArgumentLookup(
            InterfaceFieldDefinition definition,
            Dictionary<ParameterInfo, string> argumentNames)
        {
            foreach (var argument in definition.Arguments)
            {
                if (argument.Parameter is not null)
                {
                    argumentNames[argument.Parameter] = argument.Name;
                }
            }
        }
    }

    private void CompleteTypes()
    {
        _interceptor.OnBeforeCompleteTypes();

        ProcessTypes(Completed, type => CompleteType(type));
        EnsureNoErrors();

        _interceptor.OnTypesCompleted();
        _interceptor.OnAfterCompleteTypes();
    }

    internal bool CompleteType(RegisteredType registeredType)
    {
        if (registeredType.Status is TypeStatus.Completed)
        {
            return true;
        }

        if (!registeredType.IsExtension)
        {
            registeredType.Type.CompleteType(registeredType);
            registeredType.Status = TypeStatus.Completed;
        }

        return true;
    }

    private void FinalizeTypes()
    {
        foreach (var registeredType in _typeRegistry.Types)
        {
            if (!registeredType.IsExtension)
            {
                registeredType.Type.FinalizeType(registeredType);
            }
        }
    }

    private bool ProcessTypes(
        TypeDependencyFulfilled fulfilled,
        Func<RegisteredType, bool> action)
    {
        var processed = new HashSet<TypeReference>();
        var batch = new List<RegisteredType>(GetInitialBatch(fulfilled));
        var failed = false;
        batch.Sort(DirectivesFirst.Instance);

        while (!failed && processed.Count < _typeRegistry.Count && batch.Count > 0)
        {
            foreach (var registeredType in batch)
            {
                if (!action(registeredType))
                {
                    failed = true;
                    break;
                }

                foreach (var reference in registeredType.References)
                {
                    processed.Add(reference);
                }
            }

            if (!failed)
            {
                batch.Clear();
                batch.AddRange(GetNextBatch(processed));
                batch.Sort(DirectivesFirst.Instance);
            }
        }

        if (!failed && processed.Count < _typeRegistry.Count)
        {
            foreach (var type in _typeRegistry.Types
                .Where(t => !processed.Contains(t.References[0])))
            {
                // the name might not be set at this point.
                var name = string.IsNullOrEmpty(type.Type.Name)
                    ? type.References[0].ToString()!
                    : type.Type.Name;

                IReadOnlyList<TypeReference> needed =
                    TryNormalizeDependencies(
                        type.Conditionals,
                        out var normalized,
                        out var notFound)
                        ? normalized.Except(processed).ToArray()
                        : type.Conditionals.Select(t => t.Type).ToArray();

                if (notFound != null)
                {
                    _errors.Add(
                        SchemaErrorBuilder.New()
                            .SetMessage(
                                TypeInitializer_CannotFindType,
                                string.Join(", ", notFound.Reverse()))
                            .SetTypeSystemObject(type.Type)
                            .Build());
                }

                _errors.Add(
                    SchemaErrorBuilder.New()
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
        TypeDependencyFulfilled fulfilled)
    {
        _next.Clear();

        foreach (var registeredType in _typeRegistry.Types)
        {
            var conditional = false;
            registeredType.ClearConditionals();

            foreach (var dependency in registeredType.Dependencies)
            {
                if (dependency.Fulfilled == fulfilled)
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
        ISet<TypeReference> processed)
    {
        foreach (var type in _next)
        {
            if (TryNormalizeDependencies(type.Conditionals, out var normalized, out var _)
                && processed.IsSupersetOf(GetTypeRefsExceptSelfRefs(type, normalized)))
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

        List<TypeReference> GetTypeRefsExceptSelfRefs(
            RegisteredType type,
            IReadOnlyList<TypeReference> normalizedTypeReferences)
        {
            _typeRefs.Clear();
            _typeRefSet.Clear();
            _typeRefSet.UnionWith(type.References);

            foreach (var typeRef in normalizedTypeReferences)
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
        [NotNullWhen(true)] out IReadOnlyList<TypeReference>? normalized,
        [NotNullWhen(false)] out IReadOnlyList<TypeReference>? notFound)
    {
        var n = new List<TypeReference>();

        foreach (var dependency in dependencies)
        {
            if (!_typeLookup.TryNormalizeReference(dependency.Type, out var nr))
            {
                normalized = null;
                n.Add(dependency.Type);
                notFound = n;
                return false;
            }

            if (!n.Contains(nr))
            {
                n.Add(nr);
            }
        }

        normalized = n;
        notFound = null;
        return true;
    }

    private void EnsureNoErrors()
    {
        var errors = new List<ISchemaError>(_errors);

        foreach (var type in _typeRegistry.Types)
        {
            if (type.HasErrors)
            {
                errors.AddRange(type.Errors);
            }
        }

        if (errors.Count > 0)
        {
            throw new SchemaException(errors);
        }
    }

    private readonly struct RegisteredRootType(
        ITypeCompletionContext context,
        RegisteredType type,
        OperationType kind)
    {
        public ITypeCompletionContext Context { get; } = context;

        public RegisteredType Type { get; } = type;

        public OperationType Kind { get; } = kind;

        public bool IsInitialized { get; } = true;
    }

    private sealed class DirectivesFirst : IComparer<RegisteredType>
    {
        public static DirectivesFirst Instance { get; } = new();

        public int Compare(RegisteredType? x, RegisteredType? y)
        {
            if (x is null)
            {
                if (y is null)
                {
                    return 0;
                }

                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            if (x.Kind == TypeKind.Directive)
            {
                if (y.Kind == TypeKind.Directive)
                {
                    return 0;
                }

                return -1;
            }

            if (y.Kind == TypeKind.Directive)
            {
                return 1;
            }

            return 0;
        }
    }
}
