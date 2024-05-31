using System.Threading.Tasks;
using HotChocolate.Types.Relay;

namespace HotChocolate.Types;

[ObjectType<Person>]
public static partial class PersonLastName
{
    public static string LastName => default!;

    public static async Task<string> GetAddressAsync(this Person person, int someArg)
        => await Task.FromResult("something");

    [Query]
    public static string GetFooBarBaz() => "hello";

    [NodeResolver]
    public static Person GetPersonById(int id)
    {
        return new Person();
    }
}
