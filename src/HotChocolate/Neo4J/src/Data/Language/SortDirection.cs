namespace HotChocolate.Data.Neo4J.Language
{
    public class SortDirection : Visitable
    {
        private readonly string _symbol;

        public SortDirection(string symbol)
        {
            _symbol = symbol;
        }

        public override ClauseKind Kind => ClauseKind.SortDirection;

        public string GetSymbol() => _symbol;

        public static readonly SortDirection Undefined = new("");
        public static readonly SortDirection Ascending = new("ASC");
        public static readonly SortDirection Descending = new("DESC");
    }
}
