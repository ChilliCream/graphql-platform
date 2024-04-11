namespace HotChocolate.Types;

[ObjectType]
public class Person : IEntity
{
    public int Id => 1;

    public string Name => default!;
}
