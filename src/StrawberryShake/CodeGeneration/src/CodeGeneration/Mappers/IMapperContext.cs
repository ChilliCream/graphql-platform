using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public interface IMapperContext
    {
        string ClientName { get; }

        /// <summary>
        /// Gets the client root namespace.
        /// This namespace is where we have all the public client APIs.
        /// </summary>
        string Namespace { get; }

        /// <summary>
        /// Gets the client state namespace.
        /// This namespace is where we have all the store related APIs.
        /// </summary>
        /// <value></value>
        string StateNamespace { get; }

        /// <summary>
        /// Gets the client request strategy.
        /// </summary>
        RequestStrategy RequestStrategy { get; }

        /// <summary>
        /// Gets the hash provider that is used to hash queries.
        /// </summary>
        IDocumentHashProvider HashProvider { get; }

        IReadOnlyList<INamedTypeDescriptor> Types { get; }

        IReadOnlyCollection<EntityTypeDescriptor> EntityTypes { get; }

        IReadOnlyCollection<OperationDescriptor> Operations { get; }

        ClientDescriptor Client { get; }

        void Register(IEnumerable<INamedTypeDescriptor> typeDescriptors);

        void Register(IEnumerable<EntityTypeDescriptor> entityTypeDescriptor);

        void Register(IEnumerable<DataTypeDescriptor> dataTypeDescriptors);

        void Register(NameString operationName, OperationDescriptor operationDescriptor);

        void Register(NameString resultBuilderName, ResultBuilderDescriptor operationDescriptor);

        void Register(ClientDescriptor clientDescriptor);

        void Register(EntityIdFactoryDescriptor entityIdFactoryDescriptor);

        void Register(DependencyInjectionDescriptor dependencyInjectionDescriptor);
    }
}
