using System.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Runtime;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using HotChocolate.Types.Introspection;
using HotChocolate.Language;

namespace HotChocolate.Configuration
{
    internal sealed class TypeRegistrar
    {
        private readonly TypeInspector _typeInspector = new TypeInspector();
        private readonly ServiceFactory _serviceFactory = new ServiceFactory();
        private readonly IDescriptorContext _descriptorContext;
        private readonly List<InitializationContext> _initContexts =
            new List<InitializationContext>();
        private readonly HashSet<InitializationContext> _handledContexts =
            new HashSet<InitializationContext>();
        private readonly List<ISchemaError> _errors = new List<ISchemaError>();
        private readonly List<ITypeReference> _unregistered =
            new List<ITypeReference>();
        private readonly IDictionary<string, object> _contextData;

        public TypeRegistrar(
            IServiceProvider services,
            IDescriptorContext descriptorContext,
            IEnumerable<ITypeReference> initialTypes,
            IDictionary<ITypeReference, ITypeReference> clrTypes,
            IDictionary<string, object> contextData)
        {
            if (initialTypes == null)
            {
                throw new ArgumentNullException(nameof(initialTypes));
            }

            if (contextData == null)
            {
                throw new ArgumentNullException(nameof(contextData));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _descriptorContext = descriptorContext
                ?? throw new ArgumentNullException(nameof(descriptorContext));
            ClrTypes = clrTypes
                ?? throw new ArgumentNullException(nameof(clrTypes));

            _unregistered.AddRange(IntrospectionTypes.All);
            _unregistered.AddRange(Directives.All);
            _unregistered.AddRange(initialTypes);
            _serviceFactory.Services = services;
            _contextData = contextData;
        }

        public ICollection<InitializationContext> InitializationContexts =>
            _initContexts;

        public ICollection<IClrTypeReference> Unresolved { get; } =
            new List<IClrTypeReference>();

        public IDictionary<ITypeReference, RegisteredType> Registerd
        { get; } = new Dictionary<ITypeReference, RegisteredType>();

        public IDictionary<ITypeReference, ITypeReference> ClrTypes { get; }

        public ICollection<ISchemaError> Errors => _errors;

        public bool Complete()
        {
            const int max = 10000;
            int tries = 0;
            bool resolved;

            do
            {
                tries++;
                CompleteSchemaTypes();
                resolved = InferSchemaTypesFromUnresolved();
            }
            while (resolved && tries < max);

            foreach (InitializationContext context in _initContexts)
            {
                _errors.AddRange(context.Errors);
            }

            if (_errors.Count == 0 && Unresolved.Count > 0)
            {
                foreach (IClrTypeReference unresolvedReference in Unresolved)
                {
                    // TODO : resources
                    _errors.Add(SchemaErrorBuilder.New()
                        .SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            "Unable to infer or resolve a schema type from the type reference `{0}`.",
                            unresolvedReference))
                        .SetExtension(
                            TypeErrorFields.Reference,
                            unresolvedReference)
                        .SetCode(TypeErrorCodes.UnresolvedTypes)
                        .Build());
                }
            }

            return Unresolved.Count == 0 && _errors.Count == 0;
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

            foreach (IClrTypeReference unresolvedType in Unresolved.ToList())
            {
                if (Scalars.TryGetScalar(unresolvedType.Type,
                    out IClrTypeReference schemaType))
                {
                    resolved = true;
                    _unregistered.Add(schemaType);
                    Unresolved.Remove(unresolvedType);
                    if (!ClrTypes.ContainsKey(unresolvedType))
                    {
                        ClrTypes.Add(unresolvedType, schemaType);
                    }
                }
                else if (SchemaTypeResolver.TryInferSchemaType(unresolvedType,
                    out schemaType))
                {
                    resolved = true;
                    _unregistered.Add(schemaType);
                    Unresolved.Remove(unresolvedType);
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
                    && str.Type is TypeSystemObjectBase tso)
                {
                    if (BaseTypes.IsNonGenericBaseType(tso.GetType()))
                    {
                        RegisterTypeSystemObject(tso, str);
                    }
                    else
                    {
                        RegisterTypeSystemObject(tso);
                    }
                }
                else if (typeReference is ISyntaxTypeReference sr
                    && Scalars.TryGetScalar(
                        sr.Type.NamedType().Name.Value,
                        out ctr))
                {
                    RegisterClrType(ctr);
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
            TypeContext typeContext =
                SchemaTypeReference.InferTypeContext(typeSystemObject);

            var internalReference = new ClrTypeReference(
                typeSystemObject.GetType(),
                typeContext);

            RegisterTypeSystemObject(typeSystemObject, internalReference);
        }

        private void RegisterTypeSystemObject(
            TypeSystemObjectBase typeSystemObject,
            ITypeReference internalReference)
        {
            TypeContext typeContext =
                SchemaTypeReference.InferTypeContext(typeSystemObject);

            if (!Registerd.ContainsKey(internalReference))
            {
                RegisteredType registeredType =
                    InitializeAndRegister(internalReference, typeSystemObject);

                if (registeredType.ClrType != typeof(object))
                {
                    var clrTypeRef = new ClrTypeReference(
                        registeredType.ClrType,
                        typeContext);

                    RemoveUnresolvedType(clrTypeRef);

                    if (!ClrTypes.ContainsKey(clrTypeRef))
                    {
                        ClrTypes.Add(clrTypeRef, internalReference);
                    }
                }
            }
        }

        private void RemoveUnresolvedType(IClrTypeReference clrTypeRef)
        {
            Unresolved.Remove(clrTypeRef);

            if (clrTypeRef.Context == TypeContext.None)
            {
                Unresolved.Remove(new ClrTypeReference(
                    clrTypeRef.Type, TypeContext.Input));
                Unresolved.Remove(new ClrTypeReference(
                    clrTypeRef.Type, TypeContext.Output));
            }
        }

        private RegisteredType InitializeAndRegister(
            ITypeReference internalReference,
            TypeSystemObjectBase typeSystemObject)
        {
            try
            {
                var initializationContext = new InitializationContext(
                    typeSystemObject,
                    _serviceFactory.Services,
                    _descriptorContext,
                    _contextData);
                typeSystemObject.Initialize(initializationContext);
                _initContexts.Add(initializationContext);

                var registeredType = new RegisteredType(
                    internalReference,
                    typeSystemObject,
                    initializationContext.TypeDependencies);
                Registerd.Add(internalReference, registeredType);

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

        private bool IsTypeResolved(IClrTypeReference typeReference)
        {
            if (ClrTypes.ContainsKey(typeReference)
                || ClrTypes.Keys.Any(t => t.Equals(typeReference)))
            {
                return true;
            }
            return false;
        }


        private TypeSystemObjectBase CreateInstance(Type type) =>
            (TypeSystemObjectBase)_serviceFactory.CreateInstance(type);

        private static bool IsTypeSystemObject(Type type) =>
            typeof(TypeSystemObjectBase).IsAssignableFrom(type);
    }
}
