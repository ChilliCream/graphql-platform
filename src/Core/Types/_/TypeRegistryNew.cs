using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Runtime;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate
{
    internal sealed class TypeRegistrar_new
    {
        private readonly TypeInspector _typeInspector = new TypeInspector();
        private readonly ServiceFactory _serviceFactory = new ServiceFactory();
        private readonly DescriptorContext _descriptorContext;
        private readonly List<InitializationContext> _initContexts =
            new List<InitializationContext>();
        private readonly HashSet<InitializationContext> _handledContexts =
            new HashSet<InitializationContext>();
        private readonly List<ITypeReference> _unregistered;

        public TypeRegistrar_new(
            IEnumerable<ITypeReference> types,
            IServiceProvider services)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _unregistered = types.ToList();
            _serviceFactory.Services = services;
        }

        public ICollection<IClrTypeReference> Unresolved { get; } =
            new List<IClrTypeReference>();

        public IDictionary<ITypeReference, RegisteredType> Registerd
        { get; } = new Dictionary<ITypeReference, RegisteredType>();

        public IDictionary<ITypeReference, ITypeReference> ClrTypes { get; } =
            new Dictionary<ITypeReference, ITypeReference>();

        public void Complete()
        {
            bool resolved = false;
            do
            {
                CompleteSchemaTypes();
                resolved = InferSchemaTypesFromUnresolved();
            }
            while (resolved);
        }

        private void CompleteSchemaTypes()
        {
            while (_unregistered.Any())
            {
                InitializeTypes();
                EnqueueUnhandled();
            }
        }

        private bool InferSchemaTypesFromUnresolved()
        {
            bool resolved = false;

            foreach (IClrTypeReference unresolvedType in Unresolved)
            {
                if (SchemaTypeResolver.TryInferSchemaType(
                    unresolvedType,
                    out IClrTypeReference schemaType))
                {
                    resolved = true;
                    _unregistered.Add(schemaType);
                }
            }

            return resolved;
        }

        private void InitializeTypes()
        {
            foreach (ITypeReference typeReference in _unregistered.ToList())
            {
                if (typeReference is IClrTypeReference ctr)
                {
                    RegisterClrType(ctr);
                }
                else if (typeReference is ISchemaTypeReference str
                    && str is TypeSystemObjectBase tso)
                {
                    RegisterTypeSystemObject(tso);
                }

                _unregistered.Remove(typeReference);
            }
        }

        private void EnqueueUnhandled()
        {
            foreach (InitializationContext context in _initContexts)
            {
                if (_handledContexts.Add(context))
                {
                    _unregistered.AddRange(
                        context.TypeDependencies.Select(t => t.TypeReference));
                }
            }
        }

        private void RegisterClrType(IClrTypeReference typeReference)
        {
            if (!BaseTypes.IsNonGenericBaseType(typeReference.Type)
                && _typeInspector.TryCreate(typeReference.Type,
                    out TypeInfo typeInfo))
            {
                if (IsTypeSystemObject(typeInfo.ClrType))
                {
                    RegisterSchemaType(
                        typeInfo.ClrType,
                        typeReference.Context);
                }
                else
                {
                    var normalizedTypeRef = new ClrTypeReference(
                        typeInfo.ClrType,
                        typeReference.Context);

                    if (!IsTypeResolved(normalizedTypeRef))
                    {
                        Unresolved.Add(normalizedTypeRef);
                    }
                }
            }
        }

        private void RegisterSchemaType(Type type, TypeContext typeContext)
        {
            if (!Registerd.ContainsKey(new ClrTypeReference(type, typeContext)))
            {
                TypeSystemObjectBase typeSystemObject = CreateInstance(type);
                RegisterTypeSystemObject(typeSystemObject);
            }
        }

        private void RegisterTypeSystemObject(
            TypeSystemObjectBase typeSystemObject)
        {
            TypeContext typeContext = typeSystemObject is IType type
                ? SchemaTypeReference.InferTypeContext(type)
                : TypeContext.None;

            var internalReference = new ClrTypeReference(
                typeSystemObject.GetType(),
                typeContext);

            if (!Registerd.ContainsKey(internalReference))
            {
                RegisteredType registeredType =
                    InitializeAndRegister(internalReference, typeSystemObject);

                if (registeredType.ClrType != typeof(object))
                {
                    var clrTypeRef = new ClrTypeReference(
                        registeredType.ClrType,
                        typeContext);

                    Unresolved.Remove(clrTypeRef);

                    if (!ClrTypes.ContainsKey(clrTypeRef))
                    {
                        ClrTypes.Add(clrTypeRef, internalReference);
                    }
                }
            }
        }

        private RegisteredType InitializeAndRegister(
            IClrTypeReference internalReference,
            TypeSystemObjectBase typeSystemObject)
        {
            var initializationContext = new InitializationContext(
                typeSystemObject,
                _serviceFactory.Services);
            typeSystemObject.Initialize(initializationContext);
            _initContexts.Add(initializationContext);

            var registeredType = new RegisteredType(typeSystemObject);
            Registerd.Add(internalReference, registeredType);

            return registeredType;
        }

        private bool IsTypeResolved(IClrTypeReference typeReference) =>
            ClrTypes.ContainsKey(typeReference);

        private TypeSystemObjectBase CreateInstance(Type type) =>
            (TypeSystemObjectBase)_serviceFactory.CreateInstance(type);

        private static bool IsTypeSystemObject(Type type) =>
            typeof(TypeSystemObjectBase).IsAssignableFrom(type);
    }
}
