namespace HotChocolate.Types.Descriptors;

public class ClassWithInheritdoc(string foo) : BaseClass(foo)
{
    /// <inheritdoc />
    public override string Foo { get; } = foo;

    /// <inheritdoc />
    public override void Bar(string baz) { }
}
