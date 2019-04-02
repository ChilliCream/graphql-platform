using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Configuration.Validation;

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
        private readonly Dictionary<ITypeReference, ITypeReference> _clrTypes =
            new Dictionary<ITypeReference, ITypeReference>();
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
        private readonly List<ITypeReference> _initialTypes;
        private readonly Func<TypeSystemObjectBase, bool> _isQueryType;

        public TypeInitializer(
            IServiceProvider services,
            IEnumerable<ITypeReference> initialTypes,
            Func<TypeSystemObjectBase, bool> isQueryType)
        {
            if (initialTypes == null)
            {
                throw new ArgumentNullException(nameof(initialTypes));
            }

            _services = services
                ?? throw new ArgumentNullException(nameof(services));
            _isQueryType = isQueryType
                ?? throw new ArgumentNullException(nameof(isQueryType));
            _initialTypes = initialTypes.ToList();
        }

        public TypeInspector TypeInspector => _typeInspector;

        public IList<FieldMiddleware> GlobalComponents => _globalComps;

        public IDictionary<ITypeReference, ITypeReference> DependencyLookup =>
            _depsLup;

        public IDictionary<ITypeReference, RegisteredType> Types => _types;

        public IDictionary<FieldReference, RegisteredResolver> Resolvers =>
            _res;

        public void Initialize(Func<ISchema> schemaResolver)
        {
            if (schemaResolver == null)
            {
                throw new ArgumentNullException(nameof(schemaResolver));
            }

            RegisterTypes();
            CompileResolvers();
            RegisterImplicitInterfaceDependencies();
            if (CompleteNames(schemaResolver))
            {
                CompleteTypes();
            }


            IReadOnlyCollection<ISchemaError> errors =
                SchemaValidator.Validate(_types.Select(t => t.Value.Type));

            if (errors.Count > 0)
            {
                throw new SchemaException(errors);
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
            var typeRegistrar = new TypeRegistrar(_services, _initialTypes);
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

                foreach (ITypeReference key in typeRegistrar.ClrTypes.Keys)
                {
                    _clrTypes[key] = typeRegistrar.ClrTypes[key];
                }
                return true;
            }

            _errors.AddRange(typeRegistrar.Errors);
            return false;
        }

        private void CompileResolvers() =>
            ResolverCompiler.Compile(_res);

        private void RegisterImplicitInterfaceDependencies()
        {
            var withClrType =
                _types.Values.Where(t => t.ClrType != typeof(object)).ToList();
            var interfaceTypes =
                withClrType.Where(t => t.Type is InterfaceType).ToList();
            var objectTypes =
                withClrType.Where(t => t.Type is ObjectType).ToList();

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
                    _types[objectType.Reference] =
                        objectType.WithDependencies(dependencies);
                    dependencies = new List<TypeDependency>();
                }
            }
        }

        private bool CompleteNames(Func<ISchema> schemaResolver)
        {
            bool success = CompleteTypes(TypeDependencyKind.Named,
                registeredType =>
                {
                    InitializationContext initializationContext =
                        _initContexts.First(t =>
                            t.Type == registeredType.Type);
                    var completionContext = new CompletionContext(
                        initializationContext, this, schemaResolver);
                    _cmpCtx[registeredType] = completionContext;

                    registeredType.Type.CompleteName(completionContext);

                    if (_named.ContainsKey(registeredType.Type.Name))
                    {
                        // TODO : resources
                        _errors.Add(SchemaErrorBuilder.New()
                            .SetMessage("Duplicate name!")
                            .SetTypeSystemObject(registeredType.Type)
                            .Build());
                        return false;
                    }

                    _named[registeredType.Type.Name] = registeredType.Reference;
                    return true;
                });

            if (success)
            {
                UpdateDependencyLookup();
            }

            ThrowOnErrors();
            return success;
        }

        private void UpdateDependencyLookup()
        {
            foreach (RegisteredType registeredType in _types.Values)
            {
                TryNormalizeDependencies(
                    registeredType,
                    registeredType.Dependencies
                        .Select(t => t.TypeReference),
                    out _);
            }
        }

        private bool CompleteTypes()
        {
            bool success = CompleteTypes(TypeDependencyKind.Completed, registeredType =>
            {
                CompletionContext context = _cmpCtx[registeredType];
                context.Status = TypeStatus.Named;
                context.IsQueryType = _isQueryType.Invoke(registeredType.Type);
                registeredType.Type.CompleteType(context);
                return true;
            });

            ThrowOnErrors();
            return success;
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
                    processed.Add(registeredType.Reference);
                }

                batch.Clear();
                batch.AddRange(GetNextBatch(processed, kind));
            }

            if (processed.Count < _types.Count)
            {
                foreach (RegisteredType type in _types.Values
                    .Where(t => !processed.Contains(t.Reference)))
                {
                    // TODO : resources
                    _errors.Add(SchemaErrorBuilder.New()
                        .SetMessage(
                            "Unable to resolve `{0}` dependencies {1}.",
                            string.Join(", ", type.Dependencies
                                .Where(t => t.Kind == kind)
                                .Select(t => t.TypeReference)))
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
            return _types.Values.Where(t =>
                t.Dependencies.All(d => d.Kind != kind));
        }

        private IEnumerable<RegisteredType> GetNextBatch(
            ISet<ITypeReference> processed,
            TypeDependencyKind kind)
        {
            foreach (RegisteredType type in _types.Values)
            {
                if (!processed.Contains(type.Reference))
                {
                    IEnumerable<ITypeReference> references =
                        type.Dependencies.Where(t => t.Kind == kind)
                            .Select(t => t.TypeReference);

                    if (TryNormalizeDependencies(
                        type,
                        references,
                        out IReadOnlyList<ITypeReference> normalized))
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
                        r, out IClrTypeReference cnr))
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
            out IClrTypeReference normalized)
        {
            if (!BaseTypes.IsNonGenericBaseType(typeReference.Type)
                && _typeInspector.TryCreate(typeReference.Type,
                    out TypeInfo typeInfo))
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
                    normalized = new ClrTypeReference(
                        typeInfo.ClrType,
                        typeReference.Context);

                    if ((_clrTypes.TryGetValue(
                            normalized, out ITypeReference r)
                        || _clrTypes.TryGetValue(
                            normalized.WithoutContext(), out r))
                        && r is IClrTypeReference cr)
                    {
                        normalized = cr;
                        return true;
                    }
                }
            }

            normalized = null;
            return false;
        }

        private void ThrowOnErrors()
        {
            var errors = new List<ISchemaError>();

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
