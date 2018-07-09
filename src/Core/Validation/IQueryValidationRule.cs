using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public interface IQueryValidationRule
    {
        QueryValidationResult Validate(
            Schema schema,
            DocumentNode queryDocument);
    }
}
