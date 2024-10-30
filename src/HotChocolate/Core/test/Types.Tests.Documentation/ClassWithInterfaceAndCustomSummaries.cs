namespace HotChocolate.Types.Descriptors;

public class ClassWithInterfaceAndCustomSummaries(string foo) : IBaseInterface
{
    /// <summary>
    /// I am my own property.
    /// </summary>
    public string Foo { get; } = foo;

    /// <summary>
    /// I am my own method.
    /// </summary>
    /// <param name="baz">I am my own parameter.</param>
    public void Bar(string baz) { }
}
