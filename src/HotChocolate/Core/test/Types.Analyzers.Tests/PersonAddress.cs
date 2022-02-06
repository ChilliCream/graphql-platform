namespace HotChocolate.Types;

[ExtendObjectType(typeof(Person))]
public class PersonAddress
{
    public string Address { get; } = default!;
}
