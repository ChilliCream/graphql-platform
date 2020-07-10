using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Configuration
{
    internal sealed class TypeRegistrar
        : ITypeRegistrar
    {
        private readonly ServiceFactory _serviceFactory = new ServiceFactory();
        private readonly HashSet<ITypeReference> _unresolved = new HashSet<ITypeReference>();
        private readonly HashSet<RegisteredType> _handled = new HashSet<RegisteredType>();
        private readonly IDictionary<ITypeReference, RegisteredType> _registered;
        private readonly IDictionary<ClrTypeReference, ITypeReference> _clrTypeReferences;
        private readonly IDescriptorContext _descriptorContext;
        private readonly ITypeInterceptor _interceptor;

        public TypeRegistrar(
            IDictionary<ITypeReference, RegisteredType> registeredTypes,
            IDictionary<ClrTypeReference, ITypeReference> clrTypeReferences,
            IDescriptorContext descriptorContext,
            ITypeInterceptor interceptor,
            IServiceProvider services)
        {
            _registered = registeredTypes;
            _clrTypeReferences = clrTypeReferences;
            _descriptorContext = descriptorContext;
            _interceptor = interceptor;
            _serviceFactory.Services = services;
        }

        public void Register(TypeSystemObjectBase typeSystemObject, bool isInferred = false)
        {
            RegisteredType registeredType = InitializeType(typeSystemObject, isInferred);

            if (registeredType.References.Count > 0)
            {
                foreach (ITypeReference typeReference in registeredType.References)
                {
                    _registered[typeReference] = registeredType;
                    MarkResolved(typeReference);
                }

                if (typeSystemObject is IHasRuntimeType hasClrType
                    && hasClrType.RuntimeType != typeof(object))
                {
                    var clrRef = TypeReference.Create(
                        hasClrType.RuntimeType,
                        SchemaTypeReference.InferTypeContext(typeSystemObject));

                    var explicitBind = typeSystemObject is ScalarType scalar
                        && scalar.Bind == BindingBehavior.Explicit;

                    if(typeSystemObject is ScalarType)
                    {
                        
                    }

                    if (!explicitBind)
                    {
                        MarkResolved(clrRef);

                        if (!_clrTypeReferences.ContainsKey(clrRef))
                        {
                            _clrTypeReferences.Add(clrRef, registeredType.References[0]);
                        }
                    }
                }
            }
        }

        public void MarkUnresolved(ITypeReference typeReference)
        {
            _unresolved.Add(typeReference);
        }

        public void MarkResolved(ITypeReference typeReference)
        {
            _unresolved.Remove(typeReference);
        }

        public bool IsResolved(ITypeReference typeReference)
        {
            if (_registered.ContainsKey(typeReference))
            {
                return true;
            }

            if (typeReference is ClrTypeReference clrTypeReference)
            {
                return _clrTypeReferences.ContainsKey(clrTypeReference);
            }

            return false;
        }

        public TypeSystemObjectBase CreateInstance(Type namedSchemaType)
        {
            try
            {
                return (TypeSystemObjectBase)_serviceFactory.CreateInstance(namedSchemaType)!;
            }
            catch (Exception ex)
            {
                // todo : resources
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage(
                            "Unable to create instance of type `{0}`.",
                            namedSchemaType.FullName)
                        .SetException(ex)
                        .SetExtension(nameof(namedSchemaType), namedSchemaType)
                        .Build());
            }
        }

        public IReadOnlyCollection<ITypeReference> GetUnresolved()
        {
            return _unresolved.ToList();
        }

        public IReadOnlyCollection<ITypeReference> GetUnhandled()
        {
            // we are having a list and the hashset here to keep the order.
            var unhandled = new List<ITypeReference>();
            var registered = new HashSet<ITypeReference>();

            foreach (RegisteredType type in _registered.Values)
            {
                if (_handled.Add(type))
                {
                    foreach (ITypeReference typeReference in type.InitializationContext
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
            bool isInferred)
        {
            try
            {
                var discoveryContext = new TypeDiscoveryContext(
                    typeSystemObject,
                    _serviceFactory.Services,
                    _descriptorContext,
                    _interceptor);

                typeSystemObject.Initialize(discoveryContext);

                var references = new List<ITypeReference>();

                if (!isInferred)
                {
                    references.Add(TypeReference.Create(typeSystemObject));
                }

                if (!BaseTypes.IsNonGenericBaseType(typeSystemObject.GetType()))
                {
                    references.Add(TypeReference.Create(
                        typeSystemObject.GetType(),
                        SchemaTypeReference.InferTypeContext(typeSystemObject)));
                }

                if (typeSystemObject is IHasTypeIdentity hasTypeIdentity
                    && hasTypeIdentity.TypeIdentity is { })
                {
                    var reference = TypeReference.Create(
                        hasTypeIdentity.TypeIdentity,
                        SchemaTypeReference.InferTypeContext(typeSystemObject));

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
            if (discoveryContext.Interceptor.TryCreateScope(
                discoveryContext,
                out IReadOnlyList<TypeDependency>? dependencies))
            {
                return dependencies;
            }

            return discoveryContext.TypeDependencies;
        }
    }
}
