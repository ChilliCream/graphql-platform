using HotChocolate.Language;

namespace HotChocolate.Execution.Validation
{
    public interface IQueryValidationRule
    {
        QueryValidationResult Validate(Schema schema, DocumentNode queryDocument);
    }
}
