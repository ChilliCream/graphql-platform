using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Configuration.Validation;
using System.Reflection;
using System.Globalization;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Configuration
{
    internal class TypeInitializer
    {
        private static readonly TypeInspector _typeInspector =
            new TypeInspector();
        private readonly List<InitializationContext> _initContexts =
            new List<InitializationContext>();
        private readonly Dictionary<RegisteredType, CompletionContext> _cmpCtx =
            new Dictionary<RegisteredType, CompletionContext>();
        private readonly Dictionary<ITypeReference, RegisteredType> _types =
            new Dictionary<ITypeReference, RegisteredType>();
        private readonly Dictionary<NameString, ITypeReference> _named =
            new Dictionary<NameString, ITypeReference>();
        private readonly Dictionary<ITypeReference, ITypeReference> _depsLup =
            new Dictionary<ITypeReference, ITypeReference>();
        private readonly Dictionary<FieldReference, RegisteredResolver> _res =
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

        public TypeInitializer(
            IServiceProvider services,
            IDescriptorContext descriptorContext,
            IEnumerable<ITypeReference> initialTypes,
            IEnumerable<Type> externalResolverTypes,
            IDictionary<string, object> contextData,
            IsOfTypeFallback isOfType,
            Func<TypeSystemObjectBase, bool> isQueryType)
        {
            if (initialTypes == null)
            {
                throw new ArgumentNullException(nameof(initialTypes));
            }

            if (externalResolverTypes == null)
            {
                throw new ArgumentNullException(nameof(externalResolverTypes));
            }

            _services = services
                ?? throw new ArgumentNullException(nameof(services));
            _descriptorContext = descriptorContext
                ?? throw new ArgumentNullException(nameof(descriptorContext));
            _contextData = contextData
                ?? throw new ArgumentNullException(nameof(contextData));
            _isOfType = isOfType;
            _isQueryType = isQueryType
                ?? throw new ArgumentNullException(nameof(isQueryType));
            _externalResolverTypes = externalResolverTypes.ToList();
            _initialTypes = initialTypes.ToList();
        }

        public TypeInspector TypeInspector => _typeInspector;

        public IList<FieldMiddleware> GlobalComponents => _globalComps;

        public IDictionary<ITypeReference, ITypeReference> DependencyLookup =>
            _depsLup;

        public IDictionary<ITypeReference, RegisteredType> Types => _types;

        public IDictionary<ITypeReference, ITypeReference> ClrTypes { get; } =
            new Dictionary<ITypeReference, ITypeReference>();

        public IDictionary<FieldReference, RegisteredResolver> Resolvers =>
            _res;

        public bool TryGetType(NameString typeName, out IType type)
        {
            if (_named.TryGetValue(typeName, out ITypeReference reference)
                && Types.TryGetValue(reference, out RegisteredType registered)
                && registered.Type is IType t)
            {
                type = t;
                return true;
            }

            type = null;
            return false;
        }

        public void Initialize(
            Func<ISchema> schemaResolver,
            IReadOnlySchemaOptions options)
        {
            if (schemaResolver == null)
            {
                throw new ArgumentNullException(nameof(schemaResolver));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (RegisterTypes())
            {
                RegisterImplicitInterfaceDependencies();
                CompleteNames(schemaResolver);
                MergeTypeExtensions();
                RegisterExternalResolvers();
                CompileResolvers();
                CompleteTypes();
            }

            _errors.AddRange(SchemaValidator.Validate(
                _types.Select(t => t.Value.Type),
                options));

            if (_errors.Count > 0)
            {
                throw new SchemaException(_errors);
            }
        }

        public bool TryGetRegisteredType(
            ITypeReference reference,
            out RegisteredType registeredType)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            if (_types.TryGetValue(reference, out registeredType))
            {
                return true;
            }

            if (TryNormalizeReference(reference, out ITypeReference nr)
                && _types.TryGetValue(nr, out registeredType))
            {
                return true;
            }

            registeredType = null;
            return false;
        }

        private bool RegisterTypes()
        {
            var typeRegistrar = new TypeRegistrar(
                _services,
                _descriptorContext,
                _initialTypes,
                ClrTypes,
                _contextData);

            if (typeRegistrar.Complete())
            {
                foreach (InitializationContext context in
                    typeRegistrar.InitializationContexts)
                {
                    foreach (FieldReference reference in context.Resolvers.Keys)
                    {
                        if (!_res.ContainsKey(reference))
                        {
                            _res[reference] = context.Resolvers[reference];
                        }
                    }
                    _initContexts.Add(context);
                }

                foreach (ITypeReference key in typeRegistrar.Registerd.Keys)
                {
                    _types[key] = typeRegistrar.Registerd[key];
                }

                return true;
            }

            _errors.AddRange(typeRegistrar.Errors);
            return false;
        }

        private void MergeTypeExtensions()
        {
            var extensions = _types.Values
                .Where(t => t.Type is INamedTypeExtensionMerger)
                .Distinct()
                .ToList();

            if (extensions.Count > 0)
            {
                var types = _types.Values
                    .Where(t => t.Type is INamedType)
                    .Distinct()
                    .ToList();

                foreach (RegisteredType extension in extensions)
                {
                    RegisteredType type = types.FirstOrDefault(t =>
                        t.Type.Name.Equals(extension.Type.Name));

                    if (type != null
                        && extension.Type is INamedTypeExtensionMerger m
                        && type.Type is INamedType n)
                    {
                        // merge
                        CompletionContext context = _cmpCtx[extension];
                        context.Status = TypeStatus.Named;
                        MergeTypeExtension(context, m, n);

                        // update dependencies
                        context = _cmpCtx[type];
                        type = type.AddDependencies(extension.Dependencies);
                        type.Update(_types);
                        _cmpCtx[type] = context;
                        CopyAlternateNames(_cmpCtx[extension], context);
                    }
                }
            }
        }

        private static void MergeTypeExtension(
            ICompletionContext context,
            INamedTypeExtensionMerger extension,
            INamedType type)
        {
            if (extension.Kind != type.Kind)
            {
                throw new SchemaException(SchemaErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        TypeResources.TypeInitializer_Merge_KindDoesNotMatch,
                        type.Name))
                    .SetTypeSystemObject((ITypeSystemObject)type)
                    .Build());
            }

            extension.Merge(context, type);
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

        private void RegisterExternalResolvers()
        {
            if (_externalResolverTypes.Count == 0)
            {
                return;
            }

            Dictionary<NameString, ObjectType> types =
                _types.Select(t => t.Value.Type)
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
                    _res[fieldMember.ToFieldReference()] = resolver;
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
                return parent == null
                    || parent.ParameterType.IsAssignableFrom(sourceType);
            }

            return false;
        }

        private void CompileResolvers() =>
            ResolverCompiler.Compile(_res);

        private void RegisterImplicitInterfaceDependencies()
        {
            var withClrType = _types.Values
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
                    objectType.WithDependencies(dependencies).Update(_types);
                    dependencies = new List<TypeDependency>();
                }
            }
        }

        private void CompleteNames(Func<ISchema> schemaResolver)
        {
            bool success = CompleteTypes(TypeDependencyKind.Named,
                registeredType =>
                {
                    InitializationContext initializationContext =
                        _initContexts.First(t =>
                            t.Type == registeredType.Type);

                    var completionContext = new CompletionContext(
                        initializationContext, this,
                        _isOfType, schemaResolver);

                    _cmpCtx[registeredType] = completionContext;

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

            ThrowOnErrors();
        }

        private void UpdateDependencyLookup()
        {
            foreach (RegisteredType registeredType in _types.Values.Distinct())
            {
                TryNormalizeDependencies(
                    registeredType,
                    registeredType.Dependencies
                        .Select(t => t.TypeReference),
                    out _);
            }
        }

        private void CompleteTypes()
        {
            CompleteTypes(TypeDependencyKind.Completed, registeredType =>
            {
                CompletionContext context = _cmpCtx[registeredType];
                context.Status = TypeStatus.Named;
                context.IsQueryType = _isQueryType.Invoke(registeredType.Type);
                registeredType.Type.CompleteType(context);
                return true;
            });

            ThrowOnErrors();
        }

        private bool CompleteTypes(
            TypeDependencyKind kind,
            Func<RegisteredType, bool> action)
        {
            var processed = new HashSet<ITypeReference>();
            var batch = new List<RegisteredType>(
                GetInitialBatch(kind));
            bool failed = false;

            while (!failed && processed.Count < _types.Count && batch.Count > 0)
            {
                foreach (RegisteredType registeredType in batch)
                {
                    if (!action(registeredType))
                    {
                        failed = true;
                        break;
                    }

                    foreach (ITypeReference reference in
                        registeredType.References)
                    {
                        processed.Add(reference);
                    }
                }

                if (!failed)
                {
                    batch.Clear();
                    batch.AddRange(GetNextBatch(processed, kind));
                }
            }

            if (!failed && processed.Count < _types.Count)
            {
                foreach (RegisteredType type in _types.Values.Distinct()
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
            TypeDependencyKind kind)
        {
            return _types.Values
                .Where(t => t.Dependencies.All(d => d.Kind != kind))
                .Distinct();
        }

        private IEnumerable<RegisteredType> GetNextBatch(
            ISet<ITypeReference> processed,
            TypeDependencyKind kind)
        {
            foreach (RegisteredType type in _types.Values.Distinct())
            {
                if (!processed.Contains(type.References[0]))
                {
                    IEnumerable<ITypeReference> references =
                        type.Dependencies.Where(t => t.Kind == kind)
                            .Select(t => t.TypeReference);

                    if (TryNormalizeDependencies(
                        type,
                        references,
                        out IReadOnlyList<ITypeReference> normalized)
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
            out IReadOnlyList<ITypeReference> normalized)
        {
            var n = new List<ITypeReference>();

            foreach (ITypeReference reference in dependencies)
            {
                if (!TryNormalizeReference(reference, out ITypeReference nr))
                {
                    normalized = null;
                    return false;
                }
                _depsLup[reference] = nr;
                n.Add(nr);
            }

            normalized = n;
            return true;
        }

        internal bool TryNormalizeReference(
            ITypeReference typeReference,
            out ITypeReference normalized)
        {
            if (_depsLup.TryGetValue(typeReference, out ITypeReference nr))
            {
                normalized = nr;
                return true;
            }

            switch (typeReference)
            {
                case IClrTypeReference r:
                    if (TryNormalizeClrReference(
                        r, out ITypeReference cnr))
                    {
                        _depsLup[typeReference] = cnr;
                        normalized = cnr;
                        return true;
                    }
                    break;

                case ISchemaTypeReference r:
                    var internalReference = new ClrTypeReference(
                        r.Type.GetType(), r.Context);
                    _depsLup[typeReference] = internalReference;
                    normalized = internalReference;
                    return true;

                case ISyntaxTypeReference r:
                    if (_named.TryGetValue(
                        r.Type.NamedType().Name.Value,
                        out ITypeReference snr))
                    {
                        _depsLup[typeReference] = snr;
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
            out ITypeReference normalized)
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

        private void ThrowOnErrors()
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
