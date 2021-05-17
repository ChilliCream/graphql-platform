namespace HotChocolate.Data.Neo4J.Language
{
    public class Asterisk : Literal<string>
    {
        private Asterisk() : base("*")
        {
        }

        public override string AsString() => GetContent();

        public static Asterisk Instance { get; } = new();
    }
}
