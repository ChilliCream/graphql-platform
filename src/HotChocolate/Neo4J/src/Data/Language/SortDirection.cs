namespace HotChocolate.Data.Neo4J.Language
{
    public class SortDirection : Visitable
    {
        public SortDirection(string symbol)
        {
            Symbol = symbol;
        }

        public override ClauseKind Kind => ClauseKind.SortDirection;

        public string Symbol { get; }

        public static SortDirection Undefined { get; } = new("");

        public static SortDirection Ascending { get; } = new("ASC");

        public static SortDirection Descending { get; } = new("DESC");
    }
}
