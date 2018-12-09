using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public interface IQueryValidator
    {
        QueryValidationResult Validate(DocumentNode query);
    }
}
