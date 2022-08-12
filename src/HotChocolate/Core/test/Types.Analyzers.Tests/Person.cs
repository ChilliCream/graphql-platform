namespace HotChocolate.Types;

[ObjectType]
public class Person : IEntity
{
    public string Name { get; } = default!;
}
