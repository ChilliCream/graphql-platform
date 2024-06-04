namespace HotChocolate.Types;

public static class RootTypeTests
{
    [Query]
    public static string Foo() => "foo";
    
    [Mutation]
    public static string Bar() => "bar";
}