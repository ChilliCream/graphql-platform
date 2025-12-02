using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Configuration;

internal sealed class TypeDiscoverer
{
    private readonly List<ISchemaError> _errors = [];
    private readonly List<TypeReference> _resolved = [];
    private readonly IDescriptorContext _context;
    private readonly TypeRegistry _typeRegistry;
    private readonly TypeRegistrar _typeRegistrar;
    private readonly ITypeRegistrarHandler[] _handlers;
    private readonly TypeInterceptor _interceptor;

    private readonly PriorityQueue<TypeReference, (TypeReferenceStrength, int)> _unregistered = new();
    private int _nextTypeRefIndex;

    public TypeDiscoverer(
        IDescriptorContext context,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        IEnumerable<TypeReference> initialTypes,
        TypeInterceptor interceptor,
        bool includeSystemTypes = true)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(typeRegistry);
        ArgumentNullException.ThrowIfNull(typeLookup);
        ArgumentNullException.ThrowIfNull(initialTypes);
        ArgumentNullException.ThrowIfNull(interceptor);

        _context = context;
        _typeRegistry = typeRegistry;

        if (includeSystemTypes)
        {
            IntrospectionTypeReferences.Enqueue(_unregistered, context, ref _nextTypeRefIndex);
            BuiltInDirectiveTypeReferences.Enqueue(_unregistered, context, ref _nextTypeRefIndex);
        }

        foreach (var typeRef in typeRegistry.GetTypeRefs().Concat(initialTypes.Distinct()))
        {
            switch (typeRef)
            {
                case ExtendedTypeReference { Type.IsSchemaType: true } extendedTypeRef:
                    _unregistered.Enqueue(
                        typeRef,
                        (typeof(ScalarType).IsAssignableFrom(extendedTypeRef.Type.Type)
                            ? TypeReferenceStrength.VeryStrong
                            : TypeReferenceStrength.Strong,
                            _nextTypeRefIndex++));
                    break;

                case ExtendedTypeReference:
                    _unregistered.Enqueue(typeRef, (TypeReferenceStrength.Weak, _nextTypeRefIndex++));
                    break;

                case SchemaTypeReference { Type: ScalarType }:
                    _unregistered.Enqueue(typeRef, (TypeReferenceStrength.VeryStrong, _nextTypeRefIndex++));
                    break;

                case SchemaTypeReference:
                    _unregistered.Enqueue(typeRef, (TypeReferenceStrength.Strong, _nextTypeRefIndex++));
                    break;

                default:
                    _unregistered.Enqueue(typeRef, (TypeReferenceStrength.VeryWeak, _nextTypeRefIndex++));
                    break;
            }
        }

        _typeRegistrar = new TypeRegistrar(context, typeRegistry, typeLookup, interceptor);

        _handlers =
        [
            new ExtendedTypeReferenceHandler(context.TypeInspector),
            new SchemaTypeReferenceHandler(),
            new SyntaxTypeReferenceHandler(context),
            new SyntaxFactoryTypeReferenceHandler(context),
            new DependantFactoryTypeReferenceHandler(context),
            new SourceGeneratorTypeReferenceHandler(context, _typeRegistry),
            new ExtendedTypeDirectiveReferenceHandler(context.TypeInspector)
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
            foreach (var typeReference in _interceptor.RegisterMoreTypes(_typeRegistry.Types))
            {
                if (processed.Add(typeReference))
                {
                    _unregistered.Enqueue(typeReference, (TypeReferenceStrength.VeryWeak, _nextTypeRefIndex++));
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
            while (_unregistered.TryDequeue(out var typeRef, out _))
            {
                var index = (int)typeRef.Kind;
                if (_handlers.Length > index)
                {
                    _handlers[index].Handle(_typeRegistrar, typeRef);
                }
            }

            _unregistered.EnqueueRange(
                _typeRegistrar.GetUnhandled().Select(
                    typeRef => (t: typeRef, (TypeReferenceStrength.VeryWeak, _nextTypeRefIndex++))));
        }
    }

    private bool TryInferTypes()
    {
        var inferred = false;

        foreach (var unresolvedTypeRef in _typeRegistrar.Unresolved)
        {
            // first we will check if we have a type binding for the unresolved type.
            // type bindings are types that will be registered instead of the actual discovered type.
            if (unresolvedTypeRef is ExtendedTypeReference extendedTypeRef
                && _typeRegistry.RuntimeTypeRefs.TryGetValue(extendedTypeRef, out var typeReference))
            {
                inferred = true;
                _unregistered.Enqueue(typeReference, (TypeReferenceStrength.VeryWeak, _nextTypeRefIndex++));
                _resolved.Add(unresolvedTypeRef);
                continue;
            }

            // if we do not have a type binding or if we have a directive we will try to infer the type.
            if (unresolvedTypeRef is ExtendedTypeReference or ExtendedTypeDirectiveReference
                && _context.TryInferSchemaType(unresolvedTypeRef, out var schemaTypeRefs))
            {
                inferred = true;

                foreach (var schemaTypeRef in schemaTypeRefs)
                {
                    _unregistered.Enqueue(schemaTypeRef, (TypeReferenceStrength.VeryWeak, _nextTypeRefIndex++));

                    if (unresolvedTypeRef is ExtendedTypeReference typeRef)
                    {
                        // we normalize the type context so that we can correctly look up
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
                        .SetExtension("involvedTypes", types.ConvertAll(t => t.Type));
                }

                _errors.Add(builder.Build());
            }
        }
    }
}
