using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public interface IQueryValidationRule
    {
        QueryValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument);
    }
}
