using System;
using System.Collections.Generic;
using HotChocolate.Runtime;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate
{
    /*
    internal sealed class TypeRegistrar_new
    {
        public IReadOnlyList<TypeSystemObjectContext> InitializeTypes(
            SchemaBuilderContext builderContext)
        {
            var contexts = new List<TypeSystemObjectContext>();
            var unresolved = new List<TypeSystemObjectContext>();

            do
            {
                foreach (TypeSystemObjectContext typeContext in unresolved)
                {
                    typeContext.Type.Initialize(typeContext);
                }

                contexts.AddRange(unresolved);
                unresolved.Clear();

                unresolved.AddRange(builderContext
                    .GetTypeSystemObjects(TypeStatus.Uninitialized)
                    .Select(t => new TypeSystemObjectContext(builderContext, t)));
            }
            while (unresolved.Any());

            return contexts;
        }

        public void CompleteNames()
        {

        }

    }
     */

    internal class TypeRegistryNew
    {
        private readonly TypeInspector _typeInspector = new TypeInspector();
        private readonly ServiceFactory _serviceFactory = new ServiceFactory();


        public TypeRegistryNew(IServiceProvider services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _serviceFactory.Services = services;
        }

        public IDictionary<IClrTypeReference, RegisteredType> Types { get; } =
            new Dictionary<IClrTypeReference, RegisteredType>();

        public IDictionary<IClrTypeReference, IClrTypeReference> ClrTypes
        { get; } = new Dictionary<IClrTypeReference, IClrTypeReference>();

        public ISet<IClrTypeReference> Unresolved { get; } =
            new HashSet<IClrTypeReference>();

        public void RegisterDependency(
            INamedType dependant,
            ITypeReference reference,
            TypeDependencyKind kind)
        {
            switch (reference)
            {
                case IClrTypeReference clr:
                    RegisterClrDependency(dependant, clr, kind);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        private void RegisterClrDependency(
            INamedType dependant,
            IClrTypeReference reference,
            TypeDependencyKind kind)
        {
            if (!BaseTypes.IsNonGenericBaseType(reference.Type)
                && _typeInspector.TryCreate(reference.Type,
                    out TypeInfo typeInfo))
            {
                IClrTypeReference normalizedTypeRef;

                if (IsTypeSystemObject(typeInfo.ClrType))
                {
                    normalizedTypeRef = RegisterTypeSystemObject(
                        typeInfo.ClrType,
                        reference.Context);
                }
                else
                {
                    normalizedTypeRef = new ClrTypeReference(
                        typeInfo.ClrType, reference.Context);

                    if (!IsTypeResolved(normalizedTypeRef))
                    {
                        Unresolved.Add(normalizedTypeRef);
                    }
                }

                RegisteredType registeredType = GetRegisterdType(dependant);
                registeredType?.Dependencies.Add(new TypeDependency(
                    normalizedTypeRef, kind));
            }
        }

        private IClrTypeReference RegisterTypeSystemObject(
            Type type,
            TypeContext typeContext)
        {
            var internalReference = new ClrTypeReference(type, typeContext);
            if (Types.ContainsKey(internalReference))
            {
                return internalReference;
            }

            var registeredType = new RegisteredType(CreateInstance(type));
            Types.Add(internalReference, registeredType);

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

            return internalReference;
        }

        private RegisteredType GetRegisterdType(INamedType dependant)
        {
            foreach (RegisteredType registeredType in Types.Values)
            {
                if (ReferenceEquals(registeredType.Type, dependant))
                {
                    return registeredType;
                }
            }
            return null;
        }

        private bool IsTypeResolved(IClrTypeReference typeReference) =>
            ClrTypes.ContainsKey(typeReference);

        private TypeSystemObjectBase CreateInstance(Type type) =>
            (TypeSystemObjectBase)_serviceFactory.CreateInstance(type);

        private static bool IsTypeSystemObject(Type type) =>
            typeof(TypeSystemObjectBase).IsAssignableFrom(type);
    }
}
