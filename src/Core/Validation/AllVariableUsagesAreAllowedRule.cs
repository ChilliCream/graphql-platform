namespace HotChocolate.Validation
{
    internal sealed class AllVariableUsagesAreAllowedRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new AllVariableUsagesAreAllowedVisitor(schema);
        }
    }
}
