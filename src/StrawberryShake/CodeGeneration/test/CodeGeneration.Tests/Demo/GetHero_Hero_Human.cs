namespace Foo
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHero_Hero_Human
        : IGetHero_Hero_Human
    {
        public GetHero_Hero_Human(
            global::System.String name,
            global::System.Collections.Generic.IReadOnlyList<Episode?>? appearsIn)
        {
            Name = name;
            AppearsIn = appearsIn;
        }

        public global::System.String Name { get; }

        public global::System.Collections.Generic.IReadOnlyList<Episode?>? AppearsIn { get; }
    }
}
