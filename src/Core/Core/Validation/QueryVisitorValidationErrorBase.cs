using System;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    internal abstract class QueryVisitorValidationErrorBase
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

            QueryVisitorErrorBase visitor = CreateVisitor(schema);
            visitor.VisitDocument(queryDocument);

            if (visitor.Errors.Count == 0)
            {
                return QueryValidationResult.OK;
            }

            return new QueryValidationResult(visitor.Errors);
        }

        protected abstract QueryVisitorErrorBase CreateVisitor(ISchema schema);
    }

}
