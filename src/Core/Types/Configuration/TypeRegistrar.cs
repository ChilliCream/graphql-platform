using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
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
        private readonly IDictionary<IClrTypeReference, ITypeReference> _clrTypeReferences;
        private readonly IDescriptorContext _descriptorContext;
        private readonly IDictionary<string, object> _contextData;

        public TypeRegistrar(
            IDictionary<ITypeReference, RegisteredType> registeredTypes,
            IDictionary<IClrTypeReference, ITypeReference> clrTypeReferences,
            IDescriptorContext descriptorContext,
            IDictionary<string, object> contextData,
            IServiceProvider services)
        {
            _registered = registeredTypes;
            _clrTypeReferences = clrTypeReferences;
            _descriptorContext = descriptorContext;
            _contextData = contextData;
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
                    _unresolved.Remove(typeReference);
                }

                if (typeSystemObject is IHasClrType hasClrType
                    && hasClrType.ClrType != typeof(object))
                {
                    var clrRef = new ClrTypeReference(
                        hasClrType.ClrType,
                        SchemaTypeReference.InferTypeContext(typeSystemObject));
                    _unresolved.Remove(clrRef);

                    if (!_clrTypeReferences.ContainsKey(clrRef))
                    {
                        _clrTypeReferences.Add(clrRef, registeredType.References[0]);
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

            if (typeReference is IClrTypeReference clrTypeReference)
            {
                return _clrTypeReferences.ContainsKey(clrTypeReference);
            }

            return false;
        }

        public TypeSystemObjectBase CreateInstance(Type namedSchemaType)
        {
            try
            {
                return (TypeSystemObjectBase)_serviceFactory.CreateInstance(namedSchemaType);
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
            var unhandled = new List<ITypeReference>();

            foreach (RegisteredType type in _registered.Values)
            {
                if (_handled.Add(type))
                {
                    unhandled.AddRange(
                        type.InitializationContext
                            .TypeDependencies.Select(t => t.TypeReference));
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
                var initializationContext = new InitializationContext(
                    typeSystemObject,
                    _serviceFactory.Services,
                    _descriptorContext,
                    _contextData);

                typeSystemObject.Initialize(initializationContext);

                var references = new List<ITypeReference>();

                if (!isInferred)
                {
                    references.Add(new SchemaTypeReference(typeSystemObject));
                }

                if (!BaseTypes.IsNonGenericBaseType(typeSystemObject.GetType()))
                {
                    references.Add(new ClrTypeReference(
                        typeSystemObject.GetType(),
                        SchemaTypeReference.InferTypeContext(typeSystemObject)));
                }

                if (typeSystemObject is IHasTypeIdentity hasTypeIdentity
                    && hasTypeIdentity.TypeIdentity is { })
                {
                    var reference = new ClrTypeReference(
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
                    initializationContext,
                    initializationContext.TypeDependencies,
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
    }
}
