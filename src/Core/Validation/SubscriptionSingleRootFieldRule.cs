using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// Subscription operations must have exactly one root field.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Single-root-field
    /// </summary>
    internal sealed class SubscriptionSingleRootFieldRule
        : IQueryValidationRule
    {
        public QueryValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument)
        {
            var visitor = new SubscriptionSingleRootFieldVisitor(schema);
            visitor.VisitDocument(queryDocument);

            if (visitor.Errors.Count == 0)
            {
                return QueryValidationResult.OK;
            }

            return new QueryValidationResult(visitor.Errors);
        }
    }
}
