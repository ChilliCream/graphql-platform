using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public interface IQueryValidator
    {
        QueryValidationResult Validate(Schema schema, DocumentNode queryDocument);
    }
}
