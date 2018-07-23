using System;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    internal sealed class DirectivesAreInValidLocationsRule
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

            var visitor = new DirectivesAreInValidLocationsVisitor(schema);
            visitor.VisitDocument(queryDocument);

            if (visitor.Errors.Count == 0)
            {
                return QueryValidationResult.OK;
            }

            return new QueryValidationResult(visitor.Errors);
        }
    }

}
