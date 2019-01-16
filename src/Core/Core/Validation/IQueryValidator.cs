using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public interface IQueryValidator
    {
        QueryValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument);
    }
}
