using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public interface IMapperContext
    {
        string ClientName { get; }

        string Namespace { get; }
        string StateNamespace { get; }

        IReadOnlyCollection<NamedTypeDescriptor> Types { get; }

        IReadOnlyCollection<EntityTypeDescriptor> EntityTypes { get; }

        IReadOnlyCollection<EnumDescriptor> EnumTypes { get; }

        IReadOnlyCollection<OperationDescriptor> Operations { get; }

        ClientDescriptor Client { get; }

        void Register(NameString codeTypeName, NamedTypeDescriptor typeDescriptor);

        void Register(NameString codeTypeName, EntityTypeDescriptor entityTypeDescriptor);
        void Register(NameString codeTypeName, DataTypeDescriptor entityTypeDescriptor);

        void Register(NameString codeTypeName, EnumDescriptor enumTypeDescriptor);

        void Register(NameString operationName, OperationDescriptor operationDescriptor);

        void Register(NameString resultBuilderName, ResultBuilderDescriptor operationDescriptor);

        void Register(ClientDescriptor clientDescriptor);

        void Register(EntityIdFactoryDescriptor entityIdFactoryDescriptor);

        void Register(DependencyInjectionDescriptor dependencyInjectionDescriptor);
    }
}
