using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public class DefaultQueryParser
        : IQueryParser
    {
        public DocumentNode Parse(string queryText)
        {
            return Parser.Default.Parse(queryText);
        }
    }
}
