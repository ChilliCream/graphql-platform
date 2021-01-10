namespace HotChocolate.Data.Neo4J.Language
{
    public class Statement : Visitable
    {
        public override ClauseKind Kind { get; }

        public static StatementBuilder Builder() => new();
    }
}
