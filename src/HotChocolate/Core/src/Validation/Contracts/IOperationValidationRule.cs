using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public interface IOperationValidationRule
    {
        DocumentValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument,
            IReadOnlyDictionary<string, object> variableValues);
    }
}
