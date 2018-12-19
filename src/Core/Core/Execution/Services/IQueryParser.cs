using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public interface IQueryParser
    {
        DocumentNode Parse(string queryText);
    }
}
