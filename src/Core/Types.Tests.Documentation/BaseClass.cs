namespace HotChocolate.Types.Descriptors
{
#pragma warning disable 1591
    public abstract class BaseClass : BaseBaseClass
    {
        /// <inheritdoc />
        public override string Foo { get; }

        /// <inheritdoc />
        public override void Bar(string baz) { }
    }
#pragma warning restore 1587
}
