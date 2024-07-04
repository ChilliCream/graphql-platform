using HotChocolate.Types.Relay;

namespace HotChocolate.Types;

[InterfaceType("Entity")]
public interface IEntity
{
    [ID] int Id { get; }
}
