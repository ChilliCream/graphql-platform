namespace HotChocolate.Data.Neo4J.Language
{
    public class Exists : Visitable
    {
        private Exists()
        {
        }

        public override ClauseKind Kind => ClauseKind.Exists;

        public static Exists Instance { get; } = new();
    }
}
