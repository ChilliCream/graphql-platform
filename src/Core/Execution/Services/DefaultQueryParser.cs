using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public class DefaultQueryParser
        : IQueryParser
    {
        public DocumentNode Rewrite(string query)
        {
            return Parser.Default.Parse(query);
        }
    }
}
