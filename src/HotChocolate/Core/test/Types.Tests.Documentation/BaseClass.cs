namespace HotChocolate.Types.Descriptors;

public abstract class BaseClass(string foo) : BaseBaseClass
{
    /// <inheritdoc />
    public override string Foo { get; } = foo;

    /// <inheritdoc />
    public override void Bar(string baz) { }
}
