namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// The string representation of a string literal will be a quoted Cypher string in single tickmarks with
    /// escaped reserved characters.
    /// </summary>
    public class StringLiteral : Literal<string>
    {
        //public override ClauseKind Kind => ClauseKind.StringLiteral;
        public StringLiteral(string content) : base(content) { }

        public override string AsString() => $"'{GetContent()}'";
    }
}
