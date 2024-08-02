using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.Descriptors;

/// <summary>
/// Describes the dependency injection requirements of a  GraphQL client
/// </summary>
public sealed class DependencyInjectionDescriptor : ICodeDescriptor
{
    public DependencyInjectionDescriptor(
        ClientDescriptor clientDescriptor,
        IReadOnlyList<EntityTypeDescriptor> entities,
        List<OperationDescriptor> operations,
        IReadOnlyList<ITypeDescriptor> typeDescriptors,
        IReadOnlyList<TransportProfile> transportProfiles,
        EntityIdFactoryDescriptor entityIdFactoryDescriptor,
        StoreAccessorDescriptor storeAccessorDescriptor,
        IReadOnlyList<ResultFromEntityDescriptor> resultFromEntityMappers)
    {
        ClientDescriptor = clientDescriptor;
        Name = clientDescriptor.Name;
        Entities = entities;
        Operations = operations;
        TypeDescriptors = typeDescriptors;
        TransportProfiles = transportProfiles;
        EntityIdFactoryDescriptor = entityIdFactoryDescriptor;
        StoreAccessor = storeAccessorDescriptor;
        EnumTypeDescriptor = typeDescriptors.OfType<EnumTypeDescriptor>().ToList();
        ResultFromEntityMappers = resultFromEntityMappers;
    }

    /// <summary>
    /// The name of the client
    /// </summary>
    public string Name { get; }

    public ClientDescriptor ClientDescriptor { get; }

    public IReadOnlyList<EntityTypeDescriptor> Entities { get; }

    public IReadOnlyList<ITypeDescriptor> TypeDescriptors { get; }

    public IReadOnlyList<EnumTypeDescriptor> EnumTypeDescriptor { get; }

    public IReadOnlyList<TransportProfile> TransportProfiles { get; }

    public EntityIdFactoryDescriptor EntityIdFactoryDescriptor { get; }

    public StoreAccessorDescriptor StoreAccessor { get; }

    /// <summary>
    /// The operations that are contained in this client class
    /// </summary>
    public List<OperationDescriptor> Operations { get; }

    /// <summary>
    /// The result from entity mapper descriptors.
    /// </summary>
    public IReadOnlyList<ResultFromEntityDescriptor> ResultFromEntityMappers { get; }
}
