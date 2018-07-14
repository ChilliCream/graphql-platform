using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// The target field of a field selection must be defined on the scoped
    /// type of the selection set. There are no limitations on alias names.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Field-Selections-on-Objects-Interfaces-and-Unions-Types
    /// </summary>
    internal sealed class FieldMustBeDefinedRule
        : IQueryValidationRule
    {
        public QueryValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument)
        {
            var visitor = new FieldMustBeDefinedVisitor(schema);
            visitor.VisitDocument(queryDocument);

            if (visitor.Errors.Count == 0)
            {
                return QueryValidationResult.OK;
            }

            return new QueryValidationResult(visitor.Errors);
        }
    }
}
