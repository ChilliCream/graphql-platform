namespace HotChocolate.Data.Neo4J.Language
{
    public class Distinct : Visitable
    {
        public override ClauseKind Kind => ClauseKind.Distinct;
        public static readonly Distinct Instance = new ();
    }
}
