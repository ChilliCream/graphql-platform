namespace HotChocolate.Data.Neo4J.Language
{
    public class Asterisk : Literal<string>
    {
        public Asterisk() : base("*")
        {
        }

        public override string Print() => Content;

        public static Asterisk Instance { get; } = new();
    }
}
