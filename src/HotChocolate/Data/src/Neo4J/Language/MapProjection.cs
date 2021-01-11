namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// https://medium.com/neo4j/loading-graph-data-for-an-object-graph-mapper-or-graphql-5103b1a8b66e
    /// </summary>
    public class MapProjection : Expression
    {
        public override ClauseKind Kind => ClauseKind.MapProjection;
        private readonly SymbolicName _name;
        private readonly MapExpression _expression;

        public MapProjection(SymbolicName name, MapExpression expression)
        {
            _name = name;
            _expression = expression;
        }

        // public static MapProjection Create(SymbolicName name, object[] content) =>
        //    new(name, MapExpression.WithEnteries(CreateNewContent(content)));

    }
}
