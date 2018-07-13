using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// Arguments can be required. An argument is required if the argument
    /// type is non‐null and does not have a default value. Otherwise,
    /// the argument is optional.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Required-Arguments
    /// </summary>
    internal sealed class RequiredArgumentRule
        : IQueryValidationRule
    {
        public QueryValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument)
        {
            var visitor = new RequiredArgumentVisitor(schema);
            visitor.VisitDocument(queryDocument);

            if (visitor.Errors.Count == 0)
            {
                return QueryValidationResult.OK;
            }
            return new QueryValidationResult(visitor.Errors);
        }
    }
}
