using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;

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
