using HotChocolate.Language;

namespace HotChocolate.Validation
{
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
