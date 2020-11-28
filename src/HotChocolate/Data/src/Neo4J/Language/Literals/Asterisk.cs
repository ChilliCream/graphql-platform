namespace HotChocolate.Data.Neo4J.Language
{
    public sealed class Asterisk : Literal<string>
    {
        public readonly static Asterisk Instance = new Asterisk();

        private Asterisk() : base("*") { }

        public override string AsString() => GetContent();
    }
}
