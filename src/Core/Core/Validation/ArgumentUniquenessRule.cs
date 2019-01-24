using System;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// Fields and directives treat arguments as a mapping of argument name
    /// to value.
    ///
    /// More than one argument with the same name in an argument set
    /// is ambiguous and invalid.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Argument-Uniqueness
    /// </summary>
    internal sealed class ArgumentUniquenessRule
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

            var visitor = new ArgumentUniquenessVisitor(schema);
            visitor.VisitDocument(queryDocument);

            if (visitor.ViolatingNodes.Count == 0)
            {
                return QueryValidationResult.OK;
            }

            return new QueryValidationResult(
                new ValidationError(
                    $"Arguments are not unique.",
                    visitor.ViolatingNodes));
        }
    }
}
