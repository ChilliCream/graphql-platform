namespace Foo
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class FooClient
    {
        private readonly GetHeroQuery _getHeroQuery;

        public FooClient(GetHeroQuery getHeroQuery)
        {
            _getHeroQuery = getHeroQuery
                 ?? throw new global::System.ArgumentNullException(nameof(getHeroQuery));
        }

        public GetHeroQuery GetHeroQuery => _getHeroQuery;
    }
}
