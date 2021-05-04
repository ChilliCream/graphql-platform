using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration.Validation;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.Expressions;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;

#nullable enable

namespace HotChocolate.Configuration
{
    internal class TypeInitializer
    {
        private readonly Dictionary<FieldReference, RegisteredResolver> _resolvers = new();
        private readonly List<FieldMiddleware> _globalComps = new();
        private readonly List<ISchemaError> _errors = new();
        private readonly IDescriptorContext _context;
        private readonly ITypeInspector _typeInspector;
        private readonly IReadOnlyList<ITypeReference> _initialTypes;
        private readonly IReadOnlyList<Type> _externalResolverTypes;
        private readonly TypeInterceptor _interceptor;
        private readonly IsOfTypeFallback? _isOfType;
        private readonly Func<TypeSystemObjectBase, RootTypeKind> _getTypeKind;
        private readonly TypeRegistry _typeRegistry;
        private readonly TypeLookup _typeLookup;
        private readonly TypeReferenceResolver _typeReferenceResolver;

        public TypeInitializer(
            IDescriptorContext descriptorContext,
            TypeRegistry typeRegistry,
            IReadOnlyList<ITypeReference> initialTypes,
            IReadOnlyList<Type> externalResolverTypes,
            IsOfTypeFallback? isOfType,
            Func<TypeSystemObjectBase, RootTypeKind> getTypeKind)
        {
            _context = descriptorContext ??
                throw new ArgumentNullException(nameof(descriptorContext));
            _typeRegistry = typeRegistry ??
                throw new ArgumentNullException(nameof(typeRegistry));
            _initialTypes = initialTypes ??
                throw new ArgumentNullException(nameof(initialTypes));
            _externalResolverTypes = externalResolverTypes ??
                throw new ArgumentNullException(nameof(externalResolverTypes));
            _isOfType = isOfType;
            _getTypeKind = getTypeKind ??
                throw new ArgumentNullException(nameof(_getTypeKind));
            _interceptor = descriptorContext.TypeInterceptor;
            _typeInspector = descriptorContext.TypeInspector;
            _typeLookup = new TypeLookup(_typeInspector, _typeRegistry);
            _typeReferenceResolver = new TypeReferenceResolver(
                _typeInspector, _typeRegistry, _typeLookup);
        }

        public IList<FieldMiddleware> GlobalComponents => _globalComps;

        public IDictionary<FieldReference, RegisteredResolver> Resolvers => _resolvers;

        public void Initialize(
            Func<ISchema> schemaResolver,
            IReadOnlySchemaOptions options)
        {
            if (schemaResolver is null)
            {
                throw new ArgumentNullException(nameof(schemaResolver));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // first we are going to find and initialize all types that belong to our schema.
            DiscoverTypes();

            // now that we have the resolvers sorted and know what types our schema will roughly
            // consist of we are going to have a look if we can infer interface usage
            // from .NET classes that implement .NET interfaces.
            RegisterImplicitInterfaceDependencies();

            // with all types (implicit and explicit) known we complete the type names.
            CompleteNames(schemaResolver);

            // with the type names all known we can now build pairs to bring together types and
            // their type extensions.
            MergeTypeExtensions();

            // Now that we merged the types it is time to register the resolvers.
            RegisterResolvers();

            // external resolvers are resolvers that are defined on the schema and are associated
            // with the types after they have received a name and the extensions are removed.
            RegisterExternalResolvers();

            // with all resolvers in place we compile the once inferred from a C# member.
            CompileResolvers();

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
                    options));
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
                _interceptor.OnTypesInitialized(
                    _typeRegistry.Types.Select(t => t.DiscoveryContext).ToList());
            }

            _interceptor.OnAfterDiscoverTypes();
        }

        private void RegisterResolvers()
        {
            foreach (TypeDiscoveryContext context in
                _typeRegistry.Types.Select(t => t.DiscoveryContext))
            {
                if (context.Type is ObjectType { Definition: { } typeDef })
                {
                    typeDef.Name = context.Type.Name;

                    foreach (var fieldDef in typeDef.Fields)
                    {
                        if (TryRegisterResolver(
                            fieldDef,
                            typeDef,
                            out FieldReference? fieldRef,
                            out RegisteredResolver? registeredResolver) &&
                            !_resolvers.ContainsKey(fieldRef))
                        {
                            _resolvers.Add(fieldRef, registeredResolver);
                        }
                    }
                }
            }
        }

        private bool TryRegisterResolver(
            ObjectFieldDefinition fieldDef,
            ObjectTypeDefinition typeDef,
            [NotNullWhen(true)] out FieldReference? fieldRef,
            [NotNullWhen(true)] out RegisteredResolver? registeredResolver)
        {
            if (fieldDef.Resolver is null)
            {
                fieldRef = new FieldReference(typeDef.Name, fieldDef.Name);

                if (fieldDef.Expression is not null)
                {
                    registeredResolver = new RegisteredResolver(
                        fieldDef.ResolverType,
                        typeDef.RuntimeType,
                        fieldRef.WithExpression(fieldDef.Expression));

                    return true;
                }

                if (fieldDef.ResolverMember is not null)
                {
                    registeredResolver = new RegisteredResolver(
                        fieldDef.ResolverType,
                        typeDef.RuntimeType,
                        fieldRef.WithMember(fieldDef.ResolverMember));

                    return true;
                }

                if (fieldDef.Member is not null)
                {
                    registeredResolver = new RegisteredResolver(
                        fieldDef.ResolverType,
                        typeDef.RuntimeType,
                        fieldRef.WithMember(fieldDef.Member));

                    return true;
                }
            }

            fieldRef = null;
            registeredResolver = null;
            return false;
        }

        private void RegisterImplicitInterfaceDependencies()
        {
            var withRuntimeType = _typeRegistry.Types
                .Where(t => t.RuntimeType != typeof(object))
                .Distinct()
                .ToList();

            var interfaceTypes = withRuntimeType
                .Where(t => t.Type is InterfaceType)
                .Distinct()
                .ToList();

            var objectTypes = withRuntimeType
                .Where(t => t.Type is ObjectType)
                .Distinct()
                .ToList();

            var dependencies = new List<TypeDependency>();

            foreach (RegisteredType objectType in objectTypes)
            {
                foreach (RegisteredType interfaceType in interfaceTypes)
                {
                    if (interfaceType.RuntimeType.IsAssignableFrom(objectType.RuntimeType))
                    {
                        dependencies.Add(
                            new TypeDependency(
                                _typeInspector.GetTypeRef(
                                    interfaceType.RuntimeType,
                                    TypeContext.Output),
                                TypeDependencyKind.Completed));
                    }
                }

                if (dependencies.Count > 0)
                {
                    objectType.AddDependencies(dependencies);
                    _typeRegistry.Register(objectType);
                    dependencies.Clear();
                }
            }
        }

        private void CompleteNames(Func<ISchema> schemaResolver)
        {
            bool CompleteName(RegisteredType registeredType)
            {
                registeredType.SetCompletionContext(
                    new TypeCompletionContext(
                        registeredType.DiscoveryContext,
                        _typeReferenceResolver,
                        GlobalComponents,
                        Resolvers,
                        _isOfType,
                        schemaResolver));

                registeredType.Type.CompleteName(registeredType.CompletionContext);

                if (registeredType.IsNamedType || registeredType.IsDirectiveType)
                {
                    _typeRegistry.Register(registeredType.Type.Name, registeredType);
                }

                TypeCompletionContext context = registeredType.CompletionContext;
                RootTypeKind kind = _getTypeKind(registeredType.Type);
                context.IsQueryType = kind == RootTypeKind.Query;
                context.IsMutationType = kind == RootTypeKind.Mutation;
                context.IsSubscriptionType = kind == RootTypeKind.Subscription;
                return true;
            }

            _interceptor.OnBeforeCompleteTypeNames();

            if (ProcessTypes(TypeDependencyKind.Named, CompleteName) &&
                _interceptor.TriggerAggregations)
            {
                _interceptor.OnTypesCompletedName(
                    _typeRegistry.Types.Select(t => t.CompletionContext).ToList());
            }

            EnsureNoErrors();

            _interceptor.OnAfterCompleteTypeNames();
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

                foreach (var extension in extensions.Except(processed))
                {
                    if (extension.Type is INamedTypeExtension {
                        ExtendsType: { } extendsType } namedTypeExtension)
                    {
                        var isSchemaType = typeof(INamedType).IsAssignableFrom(extendsType);
                        extensionArray[0] = extension;

                        foreach (var possibleMatchingType in types
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
                    TypeCompletionContext context = extension.CompletionContext;
                    context.Status = TypeStatus.Named;
                    m.Merge(context, namedType);

                    // update dependencies
                    context = registeredType.CompletionContext;
                    registeredType.AddDependencies(extension.Dependencies);
                    _typeRegistry.Register(registeredType);
                    CopyAlternateNames(extension.CompletionContext, context);
                }
            }
        }

        private static void CopyAlternateNames(
            TypeCompletionContext source,
            TypeCompletionContext destination)
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
                _typeRegistry.Types.Select(t => t.Type)
                    .OfType<ObjectType>()
                    .ToDictionary(t => t.Name);

            foreach (Type type in _externalResolverTypes)
            {
                GraphQLResolverOfAttribute? attribute =
                    type.GetCustomAttribute<GraphQLResolverOfAttribute>();

                if (attribute?.TypeNames is not null)
                {
                    foreach (string typeName in attribute.TypeNames)
                    {
                        if (types.TryGetValue(typeName, out ObjectType? objectType))
                        {
                            AddResolvers(_context, objectType, type);
                        }
                    }
                }

                if (attribute?.Types is not null)
                {
                    foreach (Type sourceType in attribute.Types
                        .Where(t => !t.IsNonGenericSchemaType()))
                    {
                        ObjectType? objectType = types.Values
                            .FirstOrDefault(t =>
                                t.GetType() == sourceType ||
                                t.RuntimeType == sourceType);

                        if (objectType is not null)
                        {
                            AddResolvers(_context, objectType, type);
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
                context.TypeInspector.GetMembers(resolverType))
            {
                if (IsResolverRelevant(objectType.RuntimeType, member))
                {
                    NameString fieldName = context.Naming.GetMemberName(
                        member, MemberKind.ObjectField);
                    var fieldMember = new FieldMember(
                        objectType.Name, fieldName, member);
                    var resolver = new RegisteredResolver(
                        resolverType, objectType.RuntimeType, fieldMember);
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
                ParameterInfo? parent = m.GetParameters()
                    .FirstOrDefault(t => t.IsDefined(typeof(ParentAttribute)));
                return parent is null
                    || parent.ParameterType.IsAssignableFrom(sourceType);
            }

            return false;
        }

        private void CompileResolvers()
        {
            foreach (KeyValuePair<FieldReference, RegisteredResolver> item in _resolvers.ToArray())
            {
                RegisteredResolver registered = item.Value;
                if (registered.Field is FieldMember member)
                {
                    ResolverDescriptor descriptor = new(
                        registered.SourceType,
                        member,
                        resolverType: registered.IsSourceResolver
                            ? null 
                            : registered.ResolverType);
                    _resolvers[item.Key] = registered.WithField(
                        ResolverCompiler.Resolve.Compile(descriptor));
                }
            }
        }

        private void CompleteTypes()
        {
            bool CompleteType(RegisteredType registeredType)
            {
                if (!registeredType.IsExtension)
                {
                    TypeCompletionContext context = registeredType.CompletionContext;
                    context.Status = TypeStatus.Named;
                    RootTypeKind kind = _getTypeKind(registeredType.Type);
                    context.IsQueryType = kind == RootTypeKind.Query;
                    context.IsMutationType = kind == RootTypeKind.Mutation;
                    context.IsSubscriptionType = kind == RootTypeKind.Subscription;
                    registeredType.Type.CompleteType(context);
                }
                return true;
            }

            _interceptor.OnBeforeCompleteTypes();

            ProcessTypes(TypeDependencyKind.Completed, CompleteType);
            EnsureNoErrors();

            if (_interceptor.TriggerAggregations)
            {
                _interceptor.OnTypesCompleted(
                    _typeRegistry.Types.Select(t => t.CompletionContext).ToList());
            }

            _interceptor.OnAfterCompleteTypes();
        }

        private void FinalizeTypes()
        {
            foreach (var registeredType in _typeRegistry.Types)
            {
                if (!registeredType.IsExtension)
                {
                    registeredType.Type.FinalizeType(registeredType.CompletionContext);
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
                    batch.AddRange(GetNextBatch(processed, kind));
                }
            }

            if (!failed && processed.Count < _typeRegistry.Count)
            {
                foreach (RegisteredType type in _typeRegistry.Types
                    .Where(t => !processed.Contains(t.References[0])))
                {
                    string name = type.Type.Name.HasValue
                        ? type.Type.Name.Value
                        : type.References[0].ToString()!;

                    ITypeReference[] references =
                        type.Dependencies.Where(t => t.Kind == kind)
                            .Select(t => t.TypeReference).ToArray();

                    IReadOnlyList<ITypeReference> needed =
                        TryNormalizeDependencies(references,
                            out IReadOnlyList<ITypeReference>? normalized)
                            ? normalized.Except(processed).ToArray()
                            : references;

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
            return _typeRegistry.Types.Where(t => t.Dependencies.All(d => d.Kind != kind));
        }

        private IEnumerable<RegisteredType> GetNextBatch(
            ISet<ITypeReference> processed,
            TypeDependencyKind kind)
        {
            foreach (RegisteredType type in _typeRegistry.Types)
            {
                if (!processed.Contains(type.References[0]))
                {
                    IEnumerable<ITypeReference> references =
                        type.Dependencies.Where(t => t.Kind == kind)
                            .Select(t => t.TypeReference);

                    if (TryNormalizeDependencies(references,
                        out IReadOnlyList<ITypeReference>? normalized)
                        && processed.IsSupersetOf(normalized))
                    {
                        yield return type;
                    }
                }
            }
        }

        private bool TryNormalizeDependencies(
            IEnumerable<ITypeReference> dependencies,
            [NotNullWhen(true)] out IReadOnlyList<ITypeReference>? normalized)
        {
            var n = new List<ITypeReference>();

            foreach (ITypeReference reference in dependencies)
            {
                if (!_typeLookup.TryNormalizeReference(
                    reference,
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

            foreach (TypeDiscoveryContext context in
                _typeRegistry.Types.Select(t => t.DiscoveryContext))
            {
                errors.AddRange(context.Errors);
            }

            if (errors.Count > 0)
            {
                throw new SchemaException(errors);
            }
        }
    }

    internal enum RootTypeKind
    {
        Query,
        Mutation,
        Subscription,
        None
    }
}
