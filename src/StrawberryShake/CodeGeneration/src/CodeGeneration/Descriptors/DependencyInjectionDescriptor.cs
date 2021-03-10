using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.Descriptors
{
    /// <summary>
    /// Describes the dependency injection requirements of a  GraphQL client
    /// </summary>
    public class DependencyInjectionDescriptor : ICodeDescriptor
    {
        public DependencyInjectionDescriptor(
            NameString name,
            string @namespace,
            IReadOnlyList<EntityTypeDescriptor> entities,
            List<OperationDescriptor> operations,
            IReadOnlyList<ITypeDescriptor> typeDescriptors,
            IReadOnlyList<TransportProfile> transportProfiles,
            StoreAccessorDescriptor storeAccessorDescriptor)
        {
            RuntimeType = new(name, @namespace);
            Entities = entities;
            Operations = operations;
            TypeDescriptors = typeDescriptors;
            TransportProfiles = transportProfiles;
            StoreAccessor = storeAccessorDescriptor;
            EnumTypeDescriptor = typeDescriptors.OfType<EnumTypeDescriptor>().ToList();;
        }

        /// <summary>
        /// The name of the client
        /// </summary>
        public NameString Name => RuntimeType.Name;

        public RuntimeTypeInfo RuntimeType { get; }

        public IReadOnlyList<EntityTypeDescriptor> Entities { get; }

        public IReadOnlyList<ITypeDescriptor> TypeDescriptors { get; }

        public IReadOnlyList<EnumTypeDescriptor> EnumTypeDescriptor { get; }

        public IReadOnlyList<TransportProfile> TransportProfiles { get; }

        public StoreAccessorDescriptor StoreAccessor { get; }

        /// <summary>
        /// The operations that are contained in this client class
        /// </summary>
        public List<OperationDescriptor> Operations { get; }
    }
}
