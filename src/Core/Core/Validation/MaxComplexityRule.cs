using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    internal sealed class MaxComplexityRule
        : IOperationValidationRule
    {
        public QueryValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument,
            IReadOnlyDictionary<string, object> variableValues)
        {
            throw new System.NotImplementedException();
        }
    }

    public interface IComplexity
    {

    }
}
