namespace MarshmallowPie.GraphQL.Clients
{
    public class QueryFile
    {
        public QueryFile(string name, string sourceText)
        {
            Name = name;
            SourceText = sourceText;
        }

        public string Name { get; }

        public string SourceText { get; }
    }
}
