using System;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public class EmptySelectionSetRule
       : IQueryValidationRule
    {
        public QueryValidationResult Validate(ISchema schema, DocumentNode queryDocument)
        {
            throw new NotImplementedException();
        }
    }
}
