namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// AST representation of the DISTINCT keyword.
    /// </summary>
    public class Distinct : Visitable
    {
        public override ClauseKind Kind => ClauseKind.Distinct;
        public static readonly Distinct Instance = new ();
    }
}
