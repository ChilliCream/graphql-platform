using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed partial class TypeRegistrar : ITypeRegistrar
{
    private readonly HashSet<TypeReference> _unresolved = [];
    private readonly HashSet<RegisteredType> _handled = [];
    private readonly TypeRegistry _typeRegistry;
    private readonly TypeLookup _typeLookup;
    private readonly IDescriptorContext _context;
    private readonly TypeInterceptor _interceptor;
    private readonly IServiceProvider _schemaServices;
    private readonly IServiceProvider? _applicationServices;
    private readonly IServiceProvider _combinedServices;

    public TypeRegistrar(IDescriptorContext context,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeInterceptor typeInterceptor)
    {
        _typeRegistry = typeRegistry ??
            throw new ArgumentNullException(nameof(typeRegistry));
        _typeLookup = typeLookup ??
            throw new ArgumentNullException(nameof(typeLookup));
        _context = context ??
            throw new ArgumentNullException(nameof(context));
        _interceptor = typeInterceptor ??
            throw new ArgumentNullException(nameof(typeInterceptor));
        _schemaServices = context.Services;
        _applicationServices = context.Services.GetService<IApplicationServiceProvider>();

        _combinedServices = _applicationServices is null
            ? _schemaServices
            : new CombinedServiceProvider(_schemaServices, _applicationServices);
    }

    public void Register(
        TypeSystemObjectBase obj,
        string? scope,
        bool inferred = false,
        Action<RegisteredType>? configure = null)
    {
        if (obj is null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        var registeredType = InitializeType(obj, scope, inferred);

        configure?.Invoke(registeredType);

        if (registeredType.References.Count <= 0)
        {
            return;
        }

        RegisterTypeAndResolveReferences(registeredType);

        if (obj is not IHasRuntimeType hasRuntimeType ||
            hasRuntimeType.RuntimeType == typeof(object))
        {
            return;
        }

        var runtimeTypeRef =
            _context.TypeInspector.GetTypeRef(
                hasRuntimeType.RuntimeType,
                SchemaTypeReference.InferTypeContext(obj),
                scope);

        var explicitBind = obj is ScalarType { Bind: BindingBehavior.Explicit, };

        if (explicitBind)
        {
            return;
        }

        MarkResolved(runtimeTypeRef);
        _typeRegistry.TryRegister(runtimeTypeRef, registeredType.References[0]);
    }

    private void RegisterTypeAndResolveReferences(RegisteredType registeredType)
    {
        _typeRegistry.Register(registeredType);

        foreach (var typeReference in registeredType.References)
        {
            MarkResolved(typeReference);
        }
    }

    public void MarkUnresolved(TypeReference typeReference)
    {
        if (typeReference is null)
        {
            throw new ArgumentNullException(nameof(typeReference));
        }

        _unresolved.Add(typeReference);
    }

    public void MarkResolved(TypeReference typeReference)
    {
        if (typeReference is null)
        {
            throw new ArgumentNullException(nameof(typeReference));
        }

        _unresolved.Remove(typeReference);
    }

    public bool IsResolved(TypeReference typeReference)
    {
        if (typeReference is null)
        {
            throw new ArgumentNullException(nameof(typeReference));
        }

        return _typeRegistry.IsRegistered(typeReference);
    }

    public IReadOnlyCollection<TypeReference> Unresolved => _unresolved;

    public IReadOnlyCollection<TypeReference> GetUnhandled()
    {
        // we are having a list and the hash set here to keep the order.
        var unhandled = new List<TypeReference>();
        var registered = new HashSet<TypeReference>();

        foreach (var type in _typeRegistry.Types)
        {
            if (_handled.Add(type))
            {
                foreach (var typeDep in type.Dependencies)
                {
                    if (registered.Add(typeDep.Type))
                    {
                        unhandled.Add(typeDep.Type);
                    }
                }
            }
        }

        return unhandled;
    }

    private RegisteredType InitializeType(
        TypeSystemObjectBase typeSystemObject,
        string? scope,
        bool isInferred)
    {
        try
        {
            // first we create a reference to this type-system-object and ensure that we have
            // not already registered it.
            TypeReference instanceRef = TypeReference.Create(typeSystemObject, scope);

            if (_typeRegistry.TryGetType(instanceRef, out var registeredType))
            {
                // if we already no this object we will short-circuit here and just return the
                // already registered instance.
                return registeredType;
            }

            registeredType = new RegisteredType(
                typeSystemObject,
                isInferred,
                _typeRegistry,
                _typeLookup,
                _context,
                _interceptor,
                scope);

            // if the type-system-object is not yet pre-initialized we will start the
            // standard initialization flow.
            if (!typeSystemObject.IsInitialized)
            {
                typeSystemObject.Initialize(registeredType);
            }

            if (!isInferred)
            {
                registeredType.References.TryAdd(instanceRef);
            }

            if (!ExtendedType.Tools.IsNonGenericBaseType(typeSystemObject.GetType()))
            {
                registeredType.References.TryAdd(
                    _context.TypeInspector.GetTypeRef(
                        typeSystemObject.GetType(),
                        SchemaTypeReference.InferTypeContext(typeSystemObject),
                        scope));
            }

            if (typeSystemObject is IHasTypeIdentity { TypeIdentity: { } typeIdentity, })
            {
                var reference =
                    _context.TypeInspector.GetTypeRef(
                        typeIdentity,
                        SchemaTypeReference.InferTypeContext(typeSystemObject),
                        scope);

                registeredType.References.TryAdd(reference);
            }

            if (registeredType.IsDirectiveType && registeredType.RuntimeType != typeof(object))
            {
                var runtimeType = _context.TypeInspector.GetType(registeredType.RuntimeType);
                var runtimeTypeRef = TypeReference.CreateDirective(runtimeType);
                registeredType.References.TryAdd(runtimeTypeRef);
            }

            if (_interceptor.TryCreateScope(
                registeredType,
                out var dependencies))
            {
                registeredType.Dependencies.Clear();
                registeredType.Dependencies.AddRange(dependencies);
            }

            return registeredType;
        }
        catch (Exception ex)
        {
            throw new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(ex.Message)
                    .SetException(ex)
                    .SetTypeSystemObject(typeSystemObject)
                    .Build());
        }
    }
}
