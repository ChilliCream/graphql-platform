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
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (queryDocument == null)
            {
                throw new ArgumentNullException(nameof(queryDocument));
            }

            throw new NotImplementedException();
        }
    }
}
