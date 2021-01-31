namespace HotChocolate.Data.Neo4J.Language
{
    public class SortDirection : Visitable
    {
        public override ClauseKind Kind => ClauseKind.Default;

        public static readonly SortDirection Undefined = new("");
        public static readonly SortDirection Ascending = new("ASC");
        public static readonly SortDirection Descending = new("DESC");

        private readonly string _symbol;

        public SortDirection(string symbol)
        {
            _symbol = symbol;
        }
        public string GetSymbol() => _symbol;
    }
}
