using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Introspection;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed class TypeDiscoverer
{
    private readonly List<TypeReference> _unregistered = [];
    private readonly List<ISchemaError> _errors = [];
    private readonly List<TypeReference> _resolved = [];
    private readonly IDescriptorContext _context;
    private readonly TypeRegistry _typeRegistry;
    private readonly TypeRegistrar _typeRegistrar;
    private readonly ITypeRegistrarHandler[] _handlers;
    private readonly TypeInterceptor _interceptor;

    public TypeDiscoverer(
        IDescriptorContext context,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        IEnumerable<TypeReference> initialTypes,
        TypeInterceptor interceptor,
        bool includeSystemTypes = true)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (typeRegistry is null)
        {
            throw new ArgumentNullException(nameof(typeRegistry));
        }

        if (typeLookup is null)
        {
            throw new ArgumentNullException(nameof(typeLookup));
        }

        if (initialTypes is null)
        {
            throw new ArgumentNullException(nameof(initialTypes));
        }

        if (interceptor is null)
        {
            throw new ArgumentNullException(nameof(interceptor));
        }

        _context = context;
        _typeRegistry = typeRegistry;

        if (includeSystemTypes)
        {
            _unregistered.AddRange(IntrospectionTypes.CreateReferences(context));
            _unregistered.AddRange(Directives.CreateReferences(context));
        }

        _unregistered.AddRange(typeRegistry.GetTypeRefs());
        _unregistered.AddRange(initialTypes.Distinct());

        _typeRegistrar = new TypeRegistrar(context, typeRegistry, typeLookup, interceptor);

        _handlers =
        [
            new ExtendedTypeReferenceHandler(context.TypeInspector),
            new SchemaTypeReferenceHandler(),
            new SyntaxTypeReferenceHandler(context.TypeInspector),
            new FactoryTypeReferenceHandler(context),
            new DependantFactoryTypeReferenceHandler(context),
            new ExtendedTypeDirectiveReferenceHandler(context.TypeInspector),
        ];

        _interceptor = interceptor;
    }

    public TypeRegistrar Registrar => _typeRegistrar;

    public IReadOnlyList<ISchemaError> DiscoverTypes()
    {
        const int max = 1000;
        var processed = new HashSet<TypeReference>();

        DISCOVER:
        var tries = 0;
        var resolved = false;

        do
        {
            try
            {
                tries++;
                RegisterTypes();
                resolved = TryInferTypes();
            }
            catch (SchemaException ex)
            {
                _errors.AddRange(ex.Errors);
            }
            catch (Exception ex)
            {
                _errors.Add(
                    SchemaErrorBuilder.New()
                        .SetMessage(ex.Message)
                        .SetException(ex)
                        .Build());
            }
        } while (resolved && tries < max && _errors.Count == 0);

        if (_errors.Count == 0 && _unregistered.Count == 0)
        {
            foreach (var typeReference in
                _interceptor.RegisterMoreTypes(_typeRegistry.Types))
            {
                if (processed.Add(typeReference))
                {
                    _unregistered.Add(typeReference);
                }
            }

            if (_unregistered.Count > 0)
            {
                goto DISCOVER;
            }
        }

        CollectErrors();

        if (_errors.Count == 0)
        {
            _typeRegistry.CompleteDiscovery();
        }

        return _errors;
    }

    private void RegisterTypes()
    {
        while (_unregistered.Count > 0)
        {
            foreach (var typeRef in _unregistered)
            {
                var index = (int) typeRef.Kind;

                if (_handlers.Length > index)
                {
                    _handlers[index].Handle(_typeRegistrar, typeRef);
                }
            }

            _unregistered.Clear();
            _unregistered.AddRange(_typeRegistrar.GetUnhandled());
        }
    }

    private bool TryInferTypes()
    {
        var inferred = false;

        foreach (var unresolvedTypeRef in _typeRegistrar.Unresolved)
        {
            // first we will check if we have a type binding for the unresolved type.
            // type bindings are types that will be registered instead of the actual discovered type.
            if (unresolvedTypeRef is ExtendedTypeReference extendedTypeRef &&
                _typeRegistry.RuntimeTypeRefs.TryGetValue(extendedTypeRef, out var typeReference))
            {
                inferred = true;
                _unregistered.Add(typeReference);
                _resolved.Add(unresolvedTypeRef);
                continue;
            }

            // if we do not have a type binding or if we have a directive we will try to infer the type.
            if (unresolvedTypeRef is ExtendedTypeReference or ExtendedTypeDirectiveReference &&
                _context.TryInferSchemaType(unresolvedTypeRef, out var schemaTypeRefs))
            {
                inferred = true;

                foreach (var schemaTypeRef in schemaTypeRefs)
                {
                    _unregistered.Add(schemaTypeRef);

                    if (unresolvedTypeRef is ExtendedTypeReference typeRef)
                    {
                        // we normalize the type context so that we can correctly lookup
                        // if a type is already registered.
                        typeRef = typeRef.WithContext(schemaTypeRef.Context);
                        _typeRegistry.TryRegister(typeRef, schemaTypeRef);
                    }
                }

                _resolved.Add(unresolvedTypeRef);
            }
        }

        if (_resolved.Count > 0)
        {
            foreach (var typeRef in _resolved)
            {
                _typeRegistrar.MarkResolved(typeRef);
            }
        }

        return inferred;
    }

    private void CollectErrors()
    {
        foreach (var type in _typeRegistry.Types)
        {
            if (type.HasErrors)
            {
                _errors.AddRange(type.Errors);
            }
        }

        if (_errors.Count == 0 && _typeRegistrar.Unresolved.Count > 0)
        {
            foreach (var unresolvedReference in _typeRegistrar.Unresolved)
            {
                var types = _typeRegistry.Types.Where(
                    t => t.Dependencies.Select(d => d.Type)
                        .Any(r => r.Equals(unresolvedReference))).ToList();

                var builder =
                    SchemaErrorBuilder.New()
                        .SetMessage(
                            TypeResources.TypeRegistrar_TypesInconsistent,
                            unresolvedReference)
                        .SetExtension(
                            TypeErrorFields.Reference,
                            unresolvedReference)
                        .SetCode(ErrorCodes.Schema.UnresolvedTypes);

                if (types.Count == 1)
                {
                    builder.SetTypeSystemObject(types[0].Type);
                }
                else if (types.Count > 1)
                {
                    builder
                        .SetTypeSystemObject(types[0].Type)
                        .SetExtension("involvedTypes", types.Select(t => t.Type).ToList());
                }

                _errors.Add(builder.Build());
            }
        }
    }
}
