namespace HotChocolate.Types.Descriptors
{
#pragma warning disable 1591
    public class ClassWithInheritdocOnInterface(string foo) : IBaseInterface
    {
        /// <inheritdoc />
        public string Foo { get; } = foo;

        /// <inheritdoc />
        public void Bar(string baz) { }
    }
#pragma warning restore 1591
}
