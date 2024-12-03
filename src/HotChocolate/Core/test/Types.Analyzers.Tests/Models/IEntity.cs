using HotChocolate.Types.Relay;

namespace HotChocolate.Types;

public interface IEntity
{
    [ID] int Id { get; }
}

[InterfaceType<IEntity>]
public static partial class EntityInterface
{
    static partial void Configure(IInterfaceTypeDescriptor<IEntity> descriptor)
    {
        descriptor.Name("Entity");
    }

    public static string IdString([HotChocolate.Parent] IEntity entity) => entity.Id.ToString();
}
