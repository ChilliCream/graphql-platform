using System.Threading.Tasks;

namespace HotChocolate.Types;

[QueryType]
public class SomeQuery
{
    public IEntity? GetPerson() => new Person();

    [GraphQLType("CustomEnum")]
    public ValueTask<object?> GetEnum() => default;

    public Book GetBook() => new() { Title = "SomeTitle" };
}

[MutationType]
public static class SomeMutation
{
    public static string DoSomething() => "abc";
}


[SubscriptionType]
public static class SomeSubscription
{
    public static string OnSomething() => "abc";
}
