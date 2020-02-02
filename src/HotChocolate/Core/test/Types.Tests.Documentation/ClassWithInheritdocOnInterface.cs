namespace HotChocolate.Types.Descriptors
{
#pragma warning disable 1591
    public class ClassWithInheritdocOnInterface : IBaseInterface
    {
        /// <inheritdoc />
        public string Foo { get; }

        /// <inheritdoc />
        public void Bar(string baz) { }
    }
#pragma warning restore 1591
}
