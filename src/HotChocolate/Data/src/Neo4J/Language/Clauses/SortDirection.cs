namespace HotChocolate.Data.Neo4J.Language
{
    public class SortDirection : Visitable
    {
        private const string _undefined = "";
        private const string _asc = "ASC";
        private const string _desc = "DESC";

        private readonly string _symbol;
        public SortDirection(string symbol)
        {
            _symbol = symbol;
        }

        public static SortDirection Asc => new SortDirection(_asc);
        public static SortDirection Desc => new SortDirection(_desc);
        public static SortDirection Undefined => new SortDirection(_undefined);
        public string GetSymbol() => _symbol;
    }
}