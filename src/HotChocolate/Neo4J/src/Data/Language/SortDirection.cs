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

        public static SortDirection Undefined { get; } = new("");

        public static SortDirection Ascending { get; } = new("ASC");

        public static SortDirection Descending { get; } = new("DESC");
    }
}
