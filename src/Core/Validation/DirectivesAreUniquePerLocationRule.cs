using System;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// Directives are used to describe some metadata or behavioral change on
    /// the definition they apply to.
    ///
    /// When more than one directive of the
    /// same name is used, the expected metadata or behavior becomes ambiguous,
    /// therefore only one of each directive is allowed per location.
    ///
    /// http://facebook.github.io/graphql/draft/#sec-Directives-Are-Unique-Per-Location
    /// </summary>
    internal sealed class DirectivesAreUniquePerLocationRule
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

            var visitor = new DirectivesAreUniquePerLocationVisitor(schema);
            visitor.VisitDocument(queryDocument);

            if (visitor.Errors.Count == 0)
            {
                return QueryValidationResult.OK;
            }

            return new QueryValidationResult(visitor.Errors);
        }
    }
}
