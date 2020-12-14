namespace HotChocolate.Data.Neo4J.Language
{
    public sealed class Asterisk : Literal<string>
    {
        public static readonly Asterisk Instance = new();

        private Asterisk() : base("*") { }

        public override string AsString() => GetContent();
    }
}
