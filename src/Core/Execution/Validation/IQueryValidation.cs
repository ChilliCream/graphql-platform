using HotChocolate.Language;

namespace HotChocolate.Execution.Validation
{
    public interface IQueryValidator
    {
        QueryValidationResult Validate(Schema schema, DocumentNode queryDocument);
    }
}
