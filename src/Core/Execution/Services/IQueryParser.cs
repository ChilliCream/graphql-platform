using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public interface IQueryParser
    {
        DocumentNode Rewrite(string query);
    }
}
