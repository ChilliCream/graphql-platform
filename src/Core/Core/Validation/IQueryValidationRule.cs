using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public interface IQueryValidationRule
    {
        QueryValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument);
    }

    public interface IOperationValidationRule
    {
        QueryValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument,
            IReadOnlyDictionary<string, object> variableValues);
    }
}
