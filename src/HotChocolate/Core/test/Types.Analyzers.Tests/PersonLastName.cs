using HotChocolate.Types.Relay;

namespace HotChocolate.Types;

[ObjectType<Person>]
public static partial class PersonLastName
{
    public static string LastName => default!;

    [Query]
    public static string GetFooBarBaz() => "hello";

    [NodeResolver]
    public static Person GetPersonById(int id)
    {
        return new Person();
    }
}
