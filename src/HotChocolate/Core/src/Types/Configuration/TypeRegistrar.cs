using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Configuration
{
    internal sealed class TypeRegistrar
        : ITypeRegistrar
    {
        private readonly ServiceFactory _serviceFactory = new ServiceFactory();
        private readonly HashSet<ITypeReference> _unresolved = new HashSet<ITypeReference>();
        private readonly HashSet<RegisteredType> _handled = new HashSet<RegisteredType>();
        private readonly TypeRegistry _typeRegistry;
        private readonly IDescriptorContext _descriptorContext;
        private readonly ITypeInterceptor _interceptor;

        public TypeRegistrar(
            TypeRegistry typeRegistry,
            IDescriptorContext descriptorContext,
            ITypeInterceptor interceptor,
            IServiceProvider services)
        {
            _typeRegistry = typeRegistry;
            _descriptorContext = descriptorContext;
            _interceptor = interceptor;
            _serviceFactory.Services = services;
        }

        public void Register(
            TypeSystemObjectBase typeSystemObject,
            string? scope,
            bool isInferred = false)
        {
            RegisteredType registeredType = InitializeType(
                typeSystemObject,
                scope,
                isInferred);

            if (registeredType.References.Count > 0)
            {
                ResolveReferences(registeredType);

                if (typeSystemObject is IHasRuntimeType hasRuntimeType
                    && hasRuntimeType.RuntimeType != typeof(object))
                {
                    ExtendedTypeReference? runtimeTypeRef = 
                        _descriptorContext.TypeInspector.GetTypeRef(
                            hasRuntimeType.RuntimeType,
                            SchemaTypeReference.InferTypeContext(typeSystemObject),
                            scope: scope);

                    var explicitBind = typeSystemObject is ScalarType scalar
                        && scalar.Bind == BindingBehavior.Explicit;

                    if (!explicitBind)
                    {
                        MarkResolved(runtimeTypeRef);
                        _typeRegistry.TryRegister(runtimeTypeRef, registeredType.References[0]);
                    }
                }
            }
        }

        private void ResolveReferences(RegisteredType registeredType)
        {
            _typeRegistry.Register(registeredType);

            foreach (ITypeReference typeReference in registeredType.References)
            {
                MarkResolved(typeReference);
            }
        }

        public void MarkUnresolved(ITypeReference typeReference) =>
            _unresolved.Add(typeReference);

        public void MarkResolved(ITypeReference typeReference) =>
            _unresolved.Remove(typeReference);

        public bool IsResolved(ITypeReference typeReference) =>
            _typeRegistry.IsRegistered(typeReference);

        public TypeSystemObjectBase CreateInstance(Type namedSchemaType)
        {
            try
            {
                return (TypeSystemObjectBase)_serviceFactory.CreateInstance(namedSchemaType)!;
            }
            catch (Exception ex)
            {
                throw TypeRegistrar_CreateInstanceFailed(namedSchemaType, ex);
            }
        }

        public IReadOnlyCollection<ITypeReference> GetUnresolved() =>
            _unresolved.ToList();

        public IReadOnlyCollection<ITypeReference> GetUnhandled()
        {
            // we are having a list and the hashset here to keep the order.
            var unhandled = new List<ITypeReference>();
            var registered = new HashSet<ITypeReference>();

            foreach (RegisteredType type in _typeRegistry.Types)
            {
                if (_handled.Add(type))
                {
                    foreach (ITypeReference typeReference in type.DiscoveryContext
                        .TypeDependencies.Select(t => t.TypeReference))
                    {
                        if (registered.Add(typeReference))
                        {
                            unhandled.Add(typeReference);
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
                var discoveryContext = new TypeDiscoveryContext(
                    typeSystemObject,
                    scope,
                    _serviceFactory.Services,
                    _descriptorContext,
                    _interceptor);

                typeSystemObject.Initialize(discoveryContext);

                var references = new List<ITypeReference>();

                if (!isInferred)
                {
                    references.Add(TypeReference.Create(
                        typeSystemObject,
                        scope: scope));
                }

                if (!ExtendedType.Tools.IsNonGenericBaseType(typeSystemObject.GetType()))
                {
                    references.Add(_descriptorContext.TypeInspector.GetTypeRef(
                        typeSystemObject.GetType(),
                        SchemaTypeReference.InferTypeContext(typeSystemObject),
                        scope: scope));
                }

                if (typeSystemObject is IHasTypeIdentity hasTypeIdentity
                    && hasTypeIdentity.TypeIdentity is { })
                {
                    var reference = _descriptorContext.TypeInspector.GetTypeRef(
                        hasTypeIdentity.TypeIdentity,
                        SchemaTypeReference.InferTypeContext(typeSystemObject),
                        scope: scope);

                    if (!references.Contains(reference))
                    {
                        references.Add(reference);
                    }
                }

                var registeredType = new RegisteredType(
                    references,
                    typeSystemObject,
                    discoveryContext,
                    CollectDependencies(discoveryContext),
                    isInferred);

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

        private IReadOnlyList<TypeDependency> CollectDependencies(
            ITypeDiscoveryContext discoveryContext)
        {
            if (discoveryContext.TypeInterceptor.TryCreateScope(
                discoveryContext,
                out IReadOnlyList<TypeDependency>? dependencies))
            {
                return dependencies;
            }

            return discoveryContext.TypeDependencies;
        }
    }
}
