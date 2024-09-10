namespace HotChocolate.Types.Descriptors
{
#pragma warning disable 1591
    public class WithGenericTagsInXmlDoc(string foo)
    {
        /// <summary>These <c>are</c> <strong>some</strong> tags.</summary>
        public string Foo { get; set; } = foo;
    }
#pragma warning restore 1591
}
