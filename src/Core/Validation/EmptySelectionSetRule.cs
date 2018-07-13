using System;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    internal sealed class EmptySelectionSetRule
       : IQueryValidationRule
    {
        public QueryValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument)
        {
            throw new NotImplementedException();
        }
    }
}
