namespace HotChocolate.Types.Descriptors;

public class ClassWithInheritdocOnInterface(string foo) : IBaseInterface
{
    /// <inheritdoc />
    public string Foo { get; } = foo;

    /// <inheritdoc />
    public void Bar(string baz) { }
}
