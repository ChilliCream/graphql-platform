namespace HotChocolate.Types;

public static class RootTypeTests
{
    [QueryField]
    public static string Foo() => "foo";
    
    [MutationField]
    public static string Bar() => "bar";
}