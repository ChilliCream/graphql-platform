namespace HotChocolate.Types.Descriptors;

public class WithSeeTagInXmlDoc(string foo)
{
    /// <summary>
    /// <see langword="null"/> for the default <see cref="Record"/>.
    /// See <see cref="Record">this</see> and
    /// <see href="https://foo.com/bar/baz">this</see> at
    /// <see href="https://foo.com/bar/baz"/>.
    /// </summary>
    public string Foo { get; set; } = foo;
}
