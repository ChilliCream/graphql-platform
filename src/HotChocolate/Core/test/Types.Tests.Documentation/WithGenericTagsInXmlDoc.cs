namespace HotChocolate.Types.Descriptors;

public class WithGenericTagsInXmlDoc(string foo)
{
    /// <summary>These <c>are</c> <strong>some</strong> tags.</summary>
    public string Foo { get; set; } = foo;
}
