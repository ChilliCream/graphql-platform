namespace HotChocolate.Types.Descriptors
{
#pragma warning disable 1574
#pragma warning disable 1591
    public class WithSeeTagInXmlDoc
    {
        /// <summary>
        /// <see langword="null"/> for the default <see cref="Record"/>.
        /// See <see cref="Record">this</see> and
        /// <see href="https://foo.com/bar/baz">this</see> at
        /// <see href="https://foo.com/bar/baz"/>.
        /// </summary>
        public string Foo { get; set; }
    }
#pragma warning restore 1591
#pragma warning restore 1574
}
