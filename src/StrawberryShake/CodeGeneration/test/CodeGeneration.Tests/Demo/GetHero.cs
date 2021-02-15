namespace Foo
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHero
        : IGetHero
    {
        public GetHero(IGetHero_Hero? hero)
        {
            Hero = hero;
        }

        public IGetHero_Hero? Hero { get; }
    }
}
