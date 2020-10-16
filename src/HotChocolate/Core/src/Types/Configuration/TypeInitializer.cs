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
        private readonly Dictionary<FieldReference, RegisteredResolver> _resolvers =
            new Dictionary<FieldReference, RegisteredResolver>();
        private readonly List<FieldMiddleware> _globalComps = new List<FieldMiddleware>();
        private readonly List<ISchemaError> _errors = new List<ISchemaError>();
        private readonly IDescriptorContext _context;
        private readonly ITypeInspector _typeInspector;
        private readonly IReadOnlyList<ITypeReference> _initialTypes;
        private readonly IReadOnlyList<Type> _externalResolverTypes;
        private readonly ITypeInterceptor _interceptor;
        private readonly IsOfTypeFallback? _isOfType;
        private readonly Func<TypeSystemObjectBase, bool> _isQueryType;
        private readonly TypeRegistry _typeRegistry;
        private readonly TypeLookup _typeLookup;
        private readonly TypeReferenceResolver _typeReferenceResolver;

        public TypeInitializer(
            IDescriptorContext descriptorContext,
            TypeRegistry typeRegistry,
            IReadOnlyList<ITypeReference> initialTypes,
            IReadOnlyList<Type> externalResolverTypes,
            IsOfTypeFallback? isOfType,
            Func<TypeSystemObjectBase, bool> isQueryType)
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
            _isQueryType = isQueryType ??
                throw new ArgumentNullException(nameof(isQueryType));

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

            // next lets tell the type interceptors what types we have initialized.
            if (_interceptor.TriggerAggregations)
            {
                _interceptor.OnTypesInitialized(
                    _typeRegistry.Types.Select(t => t.DiscoveryContext).ToList());
            }

            // before we can start completing type names we need to register the field resolvers.
            RegisterResolvers();

            // now that we have the resolvers sorted and know what types our schema will roughly
            // consist of we are going to have a look if we can infer interface usage
            // from .NET classes that implement .NET interfaces.
            RegisterImplicitInterfaceDependencies();

            // with all types (implicit and explicit) known we complete the type names.
            CompleteNames(schemaResolver);

            // with the type names all known we can now build pairs to bring together types and
            // their type extensions.
            MergeTypeExtensions();

            // external resolvers are resolvers that are defined on the schema and are associated
            // with the types after they have received a name and the extensions are removed.
            RegisterExternalResolvers();

            // with all resolvers in place we compile the once inferred from a C# member.
            CompileResolvers();

            // last we complete the types. Completing types means that we will assign all
            // the fields resolving all missing parts and then making the types immutable.
            CompleteTypes();

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

        private void RegisterResolvers()
        {
            foreach (TypeDiscoveryContext context in
                _typeRegistry.Types.Select(t => t.DiscoveryContext))
            {
                foreach (FieldReference reference in context.Resolvers.Keys)
                {
                    if (!_resolvers.ContainsKey(reference))
                    {
                        _resolvers[reference] = context.Resolvers[reference];
                    }
                }
            }
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

                return true;
            }

            if (ProcessTypes(TypeDependencyKind.Named, CompleteName) &&
                _interceptor.TriggerAggregations)
            {
                _interceptor.OnTypesCompletedName(
                    _typeRegistry.Types.Select(t => t.CompletionContext).ToList());
            }

            EnsureNoErrors();
        }

        private void MergeTypeExtensions()
        {
            var extensions = _typeRegistry.Types
                .Where(t => t.IsExtension)
                .ToList();

            if (extensions.Count > 0)
            {
                var types = _typeRegistry.Types
                    .Where(t => t.IsNamedType)
                    .ToList();

                foreach (NameString typeName in extensions.Select(t => t.Type.Name).Distinct())
                {
                    RegisteredType? type = types.FirstOrDefault(t => t.Type.Name.Equals(typeName));
                    if(type is not null && type.Type is INamedType namedType)
                    {
                        MergeTypeExtension(
                            extensions.Where(t => t.Type.Name.Equals(typeName)),
                            type,
                            namedType);
                    }
                }
            }
        }

        private void MergeTypeExtension(
            IEnumerable<RegisteredType> extensions,
            RegisteredType registeredType,
            INamedType namedType)
        {
            foreach (RegisteredType extension in extensions)
            {
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

                    TypeDiscoveryContext initContext = extension.DiscoveryContext;
                    foreach (FieldReference reference in initContext.Resolvers.Keys)
                    {
                        _resolvers[reference]
                            = initContext.Resolvers[reference].WithSourceType(registeredType.RuntimeType);
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
                    ResolverDescriptor descriptor =
                        registered.IsSourceResolver
                            ? new ResolverDescriptor(
                                registered.SourceType,
                                member)
                            : new ResolverDescriptor(
                                registered.ResolverType,
                                registered.SourceType,
                                member);
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
                    context.IsQueryType = _isQueryType.Invoke(registeredType.Type);
                    registeredType.Type.CompleteType(context);
                }
                return true;
            }

            ProcessTypes(TypeDependencyKind.Completed, CompleteType);
            EnsureNoErrors();

            if (_interceptor.TriggerAggregations)
            {
                _interceptor.OnTypesCompleted(
                    _typeRegistry.Types.Select(t => t.CompletionContext).ToList());
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
}
