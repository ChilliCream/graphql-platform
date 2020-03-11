using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration.Validation;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Configuration
{
    internal class TypeInitializer
    {
        private static readonly TypeInspector _typeInspector =
            new TypeInspector();
        private readonly List<InitializationContext> _initContexts =
            new List<InitializationContext>();
        private readonly Dictionary<RegisteredType, CompletionContext> _completionContext =
            new Dictionary<RegisteredType, CompletionContext>();
        private readonly Dictionary<NameString, ITypeReference> _named =
            new Dictionary<NameString, ITypeReference>();
        private readonly Dictionary<ITypeReference, ITypeReference> _dependencyLookup =
            new Dictionary<ITypeReference, ITypeReference>();
        private readonly Dictionary<FieldReference, RegisteredResolver> _resolvers =
            new Dictionary<FieldReference, RegisteredResolver>();
        private readonly List<FieldMiddleware> _globalComps =
            new List<FieldMiddleware>();
        private readonly List<ISchemaError> _errors =
            new List<ISchemaError>();

        private readonly IServiceProvider _services;
        private readonly IDescriptorContext _descriptorContext;
        private readonly List<ITypeReference> _initialTypes;
        private readonly List<Type> _externalResolverTypes;
        private readonly IDictionary<string, object> _contextData;
        private readonly IsOfTypeFallback _isOfType;
        private readonly Func<TypeSystemObjectBase, bool> _isQueryType;
        private DiscoveredTypes? _discoveredTypes = null;

        public TypeInitializer(
            IServiceProvider services,
            IDescriptorContext descriptorContext,
            IEnumerable<ITypeReference> initialTypes,
            IEnumerable<Type> externalResolverTypes,
            IDictionary<string, object> contextData,
            IsOfTypeFallback isOfType,
            Func<TypeSystemObjectBase, bool> isQueryType)
        {
            _services = services;
            _descriptorContext = descriptorContext;
            _contextData = contextData;
            _isOfType = isOfType;
            _isQueryType = isQueryType;
            _externalResolverTypes = externalResolverTypes.ToList();
            _initialTypes = initialTypes.ToList();
        }

        public TypeInspector TypeInspector => _typeInspector;

        public IList<FieldMiddleware> GlobalComponents => _globalComps;

        public DiscoveredTypes? DiscoveredTypes => _discoveredTypes;

        public IDictionary<IClrTypeReference, ITypeReference> ClrTypes { get; } =
            new Dictionary<IClrTypeReference, ITypeReference>();

        public IDictionary<FieldReference, RegisteredResolver> Resolvers => _resolvers;

        public bool TryGetType(NameString typeName, out IType? type)
        {
            if (_discoveredTypes is { }
                && _named.TryGetValue(typeName, out ITypeReference reference)
                && _discoveredTypes.TryGetType(reference, out RegisteredType registered)
                && registered.Type is IType t)
            {
                type = t;
                return true;
            }

            type = null;
            return false;
        }

        public DiscoveredTypes Initialize(
            Func<ISchema> schemaResolver,
            IReadOnlySchemaOptions options)
        {
            var typeRegistrar = new TypeDiscoverer(
                new HashSet<ITypeReference>(_initialTypes),
                ClrTypes,
                _descriptorContext,
                _contextData,
                _services);

            _discoveredTypes = typeRegistrar.DiscoverTypes();

            if (_discoveredTypes.Errors.Count == 0)
            {
                RegisterResolvers(_discoveredTypes);
                RegisterImplicitInterfaceDependencies(_discoveredTypes);
                CompleteNames(_discoveredTypes, schemaResolver);
                MergeTypeExtensions(_discoveredTypes);
                RegisterExternalResolvers(_discoveredTypes);
                CompileResolvers();
                CompleteTypes(_discoveredTypes);
            }

            _errors.AddRange(_discoveredTypes.Errors);

            if (_errors.Count == 0)
            {
                _errors.AddRange(SchemaValidator.Validate(
                    _discoveredTypes.Types.Select(t => t.Type),
                    options));
            }

            if (_errors.Count > 0)
            {
                throw new SchemaException(_errors);
            }

            return _discoveredTypes;
        }

        public bool TryGetRegisteredType(
            ITypeReference reference,
            out RegisteredType? registeredType)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            if (_discoveredTypes is null)
            {
                registeredType = null;
                return false;
            }

            if (_discoveredTypes.TryGetType(reference, out registeredType))
            {
                return true;
            }

            if (TryNormalizeReference(reference, out ITypeReference? nr)
                && _discoveredTypes.TryGetType(nr!, out registeredType))
            {
                return true;
            }

            registeredType = null;
            return false;
        }

        private void RegisterResolvers(DiscoveredTypes discoveredTypes)
        {
            foreach (InitializationContext context in
                discoveredTypes.Types.Select(t => t.InitializationContext))
            {
                foreach (FieldReference reference in context.Resolvers.Keys)
                {
                    if (!_resolvers.ContainsKey(reference))
                    {
                        _resolvers[reference] = context.Resolvers[reference];
                    }
                }
                _initContexts.Add(context);
            }
        }

        private void MergeTypeExtensions(DiscoveredTypes discoveredTypes)
        {
            var extensions = discoveredTypes.Types
                .Where(t => t.IsExtension)
                .ToList();

            if (extensions.Count > 0)
            {
                var types = discoveredTypes.Types
                    .Where(t => t.IsNamedType)
                    .ToList();

                foreach (IGrouping<NameString, RegisteredType> group in
                    extensions.GroupBy(t => t.Type.Name))
                {
                    RegisteredType type = types.FirstOrDefault(t =>
                        t.Type.Name.Equals(group.Key));

                    if (type != null && type.Type is INamedType targetType)
                    {
                        MergeTypeExtension(discoveredTypes, group, type, targetType);
                    }
                }

                discoveredTypes.RebuildTypeSet();
            }
        }

        private void MergeTypeExtension(
            DiscoveredTypes discoveredTypes,
            IEnumerable<RegisteredType> extensions,
            RegisteredType type,
            INamedType targetType)
        {
            foreach (RegisteredType extension in extensions)
            {
                if (extension.Type is INamedTypeExtensionMerger m)
                {
                    if (m.Kind != targetType.Kind)
                    {
                        throw new SchemaException(SchemaErrorBuilder.New()
                            .SetMessage(string.Format(
                                CultureInfo.InvariantCulture,
                                TypeResources.TypeInitializer_Merge_KindDoesNotMatch,
                                targetType.Name))
                            .SetTypeSystemObject((ITypeSystemObject)targetType)
                            .Build());
                    }

                    InitializationContext initContext = extension.InitializationContext;
                    foreach (FieldReference reference in initContext.Resolvers.Keys)
                    {
                        _resolvers[reference]
                            = initContext.Resolvers[reference].WithSourceType(type.ClrType);
                    }

                    // merge
                    CompletionContext context = _completionContext[extension];
                    context.Status = TypeStatus.Named;
                    m.Merge(context, targetType);

                    // update dependencies
                    context = _completionContext[type];
                    type = type.AddDependencies(extension.Dependencies);
                    discoveredTypes.UpdateType(type);
                    _completionContext[type] = context;
                    CopyAlternateNames(_completionContext[extension], context);
                }
            }
        }

        private static void CopyAlternateNames(
            CompletionContext source,
            CompletionContext destination)
        {
            foreach (NameString name in source.AlternateTypeNames)
            {
                destination.AlternateTypeNames.Add(name);
            }
        }

        private void RegisterExternalResolvers(DiscoveredTypes discoveredTypes)
        {
            if (_externalResolverTypes.Count == 0)
            {
                return;
            }

            Dictionary<NameString, ObjectType> types =
                discoveredTypes.Types.Select(t => t.Type)
                    .OfType<ObjectType>()
                    .ToDictionary(t => t.Name);

            foreach (Type type in _externalResolverTypes)
            {
                GraphQLResolverOfAttribute attribute =
                    type.GetCustomAttribute<GraphQLResolverOfAttribute>();

                if (attribute.TypeNames != null)
                {
                    foreach (string typeName in attribute.TypeNames)
                    {
                        if (types.TryGetValue(typeName,
                            out ObjectType objectType))
                        {
                            AddResolvers(_descriptorContext, objectType, type);
                        }
                    }
                }

                if (attribute.Types != null)
                {
                    foreach (Type sourceType in attribute.Types
                        .Where(t => !BaseTypes.IsNonGenericBaseType(t)))
                    {

                        ObjectType objectType = types.Values
                            .FirstOrDefault(t => t.GetType() == sourceType
                                || t.ClrType == sourceType);
                        if (objectType != null)
                        {
                            AddResolvers(_descriptorContext, objectType, type);
                        }
                    }
                }
            }
        }

        private void AddResolvers(
            IDescriptorContext context,
            ObjectType objectType,
            Type resolverType)
        {
            foreach (MemberInfo member in
                context.Inspector.GetMembers(resolverType))
            {
                if (IsResolverRelevant(objectType.ClrType, member))
                {
                    NameString fieldName = context.Naming.GetMemberName(
                        member, MemberKind.ObjectField);
                    var fieldMember = new FieldMember(
                        objectType.Name, fieldName, member);
                    var resolver = new RegisteredResolver(
                        resolverType, objectType.ClrType, fieldMember);
                    _resolvers[fieldMember.ToFieldReference()] = resolver;
                }
            }
        }

        private static bool IsResolverRelevant(
            Type sourceType,
            MemberInfo resolver)
        {
            if (resolver is PropertyInfo)
            {
                return true;
            }

            if (resolver is MethodInfo m)
            {
                ParameterInfo parent = m.GetParameters()
                    .FirstOrDefault(t => t.IsDefined(typeof(ParentAttribute)));

                return parent == null ||
                        parent.ParameterType.IsAssignableFrom(sourceType);
            }

            return false;
        }

        private void CompileResolvers() => ResolverCompiler.Compile(_resolvers);

        private void RegisterImplicitInterfaceDependencies(DiscoveredTypes discoveredTypes)
        {
            var withClrType = discoveredTypes.Types
                .Where(t => t.ClrType != typeof(object))
                .Distinct()
                .ToList();

            var interfaceTypes = withClrType
                .Where(t => t.Type is InterfaceType)
                .Distinct()
                .ToList();

            var objectTypes = withClrType
                .Where(t => t.Type is ObjectType)
                .Distinct()
                .ToList();

            var dependencies = new List<TypeDependency>();

            foreach (RegisteredType objectType in objectTypes)
            {
                foreach (RegisteredType interfaceType in interfaceTypes)
                {
                    if (interfaceType.ClrType.IsAssignableFrom(
                        objectType.ClrType))
                    {
                        dependencies.Add(new TypeDependency(
                            new ClrTypeReference(
                                interfaceType.ClrType,
                                TypeContext.Output),
                            TypeDependencyKind.Completed));
                    }
                }

                if (dependencies.Count > 0)
                {
                    dependencies.AddRange(objectType.Dependencies);
                    discoveredTypes.UpdateType(objectType.WithDependencies(dependencies));
                    dependencies = new List<TypeDependency>();
                }
            }

            discoveredTypes.RebuildTypeSet();
        }

        private void CompleteNames(DiscoveredTypes discoveredTypes, Func<ISchema> schemaResolver)
        {
            bool success = CompleteTypes(
                discoveredTypes,
                TypeDependencyKind.Named,
                registeredType =>
                {
                    InitializationContext initializationContext =
                        _initContexts.First(t =>
                            t.Type == registeredType.Type);

                    var completionContext = new CompletionContext(
                        initializationContext, this,
                        _isOfType, schemaResolver);

                    _completionContext[registeredType] = completionContext;

                    registeredType.Type.CompleteName(completionContext);

                    if (registeredType.Type is INamedType
                        || registeredType.Type is DirectiveType)
                    {
                        if (_named.ContainsKey(registeredType.Type.Name))
                        {
                            _errors.Add(SchemaErrorBuilder.New()
                                .SetMessage(string.Format(
                                    CultureInfo.InvariantCulture,
                                    TypeResources.TypeInitializer_CompleteName_Duplicate,
                                    registeredType.Type.Name))
                                .SetTypeSystemObject(registeredType.Type)
                                .Build());
                            return false;
                        }
                        _named[registeredType.Type.Name] =
                            registeredType.References[0];
                    }

                    return true;
                });

            if (success)
            {
                UpdateDependencyLookup();
            }

            EnsureNoErrors();
        }

        private void UpdateDependencyLookup()
        {
            if (_discoveredTypes is { })
            {
                foreach (RegisteredType registeredType in _discoveredTypes.Types)
                {
                    TryNormalizeDependencies(
                        registeredType,
                        registeredType.Dependencies
                            .Select(t => t.TypeReference),
                        out _);
                }
            }
        }

        private void CompleteTypes(DiscoveredTypes discoveredTypes)
        {
            CompleteTypes(
                discoveredTypes,
                TypeDependencyKind.Completed,
                registeredType =>
                {
                    if (!registeredType.IsExtension)
                    {
                        CompletionContext context = _completionContext[registeredType];
                        context.Status = TypeStatus.Named;
                        context.IsQueryType = _isQueryType.Invoke(registeredType.Type);
                        registeredType.Type.CompleteType(context);
                    }
                    return true;
                });

            EnsureNoErrors();
        }

        private bool CompleteTypes(
            DiscoveredTypes discoveredTypes,
            TypeDependencyKind kind,
            Func<RegisteredType, bool> action)
        {
            var processed = new HashSet<ITypeReference>();
            var batch = new List<RegisteredType>(GetInitialBatch(discoveredTypes, kind));
            bool failed = false;

            while (!failed
                && processed.Count < discoveredTypes.TypeReferenceCount
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
                    batch.AddRange(GetNextBatch(discoveredTypes, processed, kind));
                }
            }

            if (!failed && processed.Count < discoveredTypes.TypeReferenceCount)
            {
                foreach (RegisteredType type in discoveredTypes.Types
                    .Where(t => !processed.Contains(t.References[0])))
                {
                    string name = type.Type.Name.HasValue
                        ? type.Type.Name.Value
                        : type.References.ToString();

                    _errors.Add(SchemaErrorBuilder.New()
                        .SetMessage(string.Format(
                            TypeResources.TypeInitializer_CannotResolveDependency,
                            name,
                            string.Join(", ", type.Dependencies
                                .Where(t => t.Kind == kind)
                                .Select(t => t.TypeReference))))
                        .SetTypeSystemObject(type.Type)
                        .Build());
                }

                return false;
            }

            return _errors.Count == 0;
        }

        private IEnumerable<RegisteredType> GetInitialBatch(
            DiscoveredTypes discoveredTypes,
            TypeDependencyKind kind)
        {
            return discoveredTypes.Types
                .Where(t => t.Dependencies.All(d => d.Kind != kind));
        }

        private IEnumerable<RegisteredType> GetNextBatch(
            DiscoveredTypes discoveredTypes,
            ISet<ITypeReference> processed,
            TypeDependencyKind kind)
        {
            foreach (RegisteredType type in discoveredTypes.Types)
            {
                if (!processed.Contains(type.References[0]))
                {
                    IEnumerable<ITypeReference> references =
                        type.Dependencies.Where(t => t.Kind == kind)
                            .Select(t => t.TypeReference);

                    if (TryNormalizeDependencies(
                        type,
                        references,
                        out IReadOnlyList<ITypeReference>? normalized)
                        && processed.IsSupersetOf(normalized))
                    {
                        yield return type;
                    }
                }
            }
        }

        private bool TryNormalizeDependencies(
            RegisteredType registeredType,
            IEnumerable<ITypeReference> dependencies,
            out IReadOnlyList<ITypeReference>? normalized)
        {
            var n = new List<ITypeReference>();

            foreach (ITypeReference reference in dependencies)
            {
                if (!TryNormalizeReference(reference, out ITypeReference? nr))
                {
                    normalized = null;
                    return false;
                }
                _dependencyLookup[reference] = nr!;
                n.Add(nr!);
            }

            normalized = n;
            return true;
        }

        internal bool TryNormalizeReference(
            ITypeReference typeReference,
            out ITypeReference? normalized)
        {
            if (_dependencyLookup.TryGetValue(typeReference, out ITypeReference nr))
            {
                normalized = nr;
                return true;
            }

            switch (typeReference)
            {
                case IClrTypeReference r:
                    if (TryNormalizeClrReference(r, out ITypeReference? cnr))
                    {
                        _dependencyLookup[typeReference] = cnr!;
                        normalized = cnr;
                        return true;
                    }
                    break;

                case ISchemaTypeReference r:
                    var internalReference = new ClrTypeReference(r.Type.GetType(), r.Context);
                    _dependencyLookup[typeReference] = internalReference;
                    normalized = internalReference;
                    return true;

                case ISyntaxTypeReference r:
                    if (_named.TryGetValue(
                        r.Type.NamedType().Name.Value,
                        out ITypeReference snr))
                    {
                        _dependencyLookup[typeReference] = snr;
                        normalized = snr;
                        return true;
                    }
                    break;
            }

            normalized = null;
            return false;
        }

        private bool TryNormalizeClrReference(
            IClrTypeReference typeReference,
            out ITypeReference? normalized)
        {
            if (!BaseTypes.IsNonGenericBaseType(typeReference.Type)
                && _typeInspector.TryCreate(typeReference.Type,
                    out Utilities.TypeInfo typeInfo))
            {
                if (IsTypeSystemObject(typeInfo.ClrType))
                {
                    normalized = new ClrTypeReference(
                        typeInfo.ClrType,
                        SchemaTypeReference.InferTypeContext(typeInfo.ClrType));
                    return true;
                }
                else
                {
                    for (int i = 0; i < typeInfo.Components.Count; i++)
                    {
                        var n = new ClrTypeReference(
                            typeInfo.Components[i],
                            typeReference.Context);

                        if ((ClrTypes.TryGetValue(
                                n, out ITypeReference r)
                            || ClrTypes.TryGetValue(
                                n.WithoutContext(), out r)))
                        {
                            normalized = r;
                            return true;
                        }
                    }
                }
            }

            normalized = null;
            return false;
        }

        private void EnsureNoErrors()
        {
            var errors = new List<ISchemaError>(_errors);

            foreach (InitializationContext context in _initContexts)
            {
                errors.AddRange(context.Errors);
            }

            if (errors.Count > 0)
            {
                throw new SchemaException(errors);
            }
        }

        private static bool IsTypeSystemObject(Type type) =>
            typeof(TypeSystemObjectBase).IsAssignableFrom(type);
    }
}
