using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public class DefaultQueryParser
        : IQueryParser
    {
        public DocumentNode Parse(string queryText)
        {
            return new Utf8GraphQLParser(
                Encoding.UTF8.GetBytes(queryText),
                ParserOptions.Default).Parse();
        }
    }
}
