namespace HotChocolate.Types.Descriptors
{
#pragma warning disable 1591
    public class ClassWithInterfaceAndCustomSummaries : IBaseInterface
    {
        /// <summary>
        /// I am my own property.
        /// </summary>
        public string Foo { get; }

        /// <summary>
        /// I am my own method.
        /// </summary>
        /// <param name="baz">I am my own parameter.</param>
        public void Bar(string baz) { }
    }
#pragma warning restore 1591
}
