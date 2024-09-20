using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.Tools.Configuration;

namespace StrawberryShake.CodeGeneration.Mappers;

public interface IMapperContext
{
    string ClientName { get; }

    /// <summary>
    /// Gets the client root namespace.
    /// This namespace is where we have all the public client APIs.
    /// </summary>
    string Namespace { get; }

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

    IReadOnlyList<TransportProfile> TransportProfiles { get; }

    ClientDescriptor Client { get; }

    StoreAccessorDescriptor StoreAccessor { get; }

    EntityIdFactoryDescriptor EntityIdFactory { get; }

    IReadOnlyList<ResultFromEntityDescriptor> ResultFromEntityMappers { get; }

    void Register(IEnumerable<INamedTypeDescriptor> typeDescriptors);

    void Register(IEnumerable<EntityTypeDescriptor> entityTypeDescriptor);

    void Register(IEnumerable<DataTypeDescriptor> dataTypeDescriptors);

    void Register(string operationName, OperationDescriptor operationDescriptor);

    void Register(string resultBuilderName, ResultBuilderDescriptor operationDescriptor);

    void Register(ClientDescriptor clientDescriptor);

    void Register(EntityIdFactoryDescriptor entityIdFactoryDescriptor);

    void Register(DependencyInjectionDescriptor dependencyInjectionDescriptor);

    void Register(StoreAccessorDescriptor storeAccessorDescriptor);

    bool Register(string typeName, TypeKind kind, RuntimeTypeInfo runtimeType);

    void Register(ResultFromEntityDescriptor descriptor);

    RuntimeTypeInfo GetRuntimeType(string typeName, TypeKind kind);

    T GetType<T>(string runtimeTypeName) where T : INamedTypeDescriptor;
}
