namespace HotChocolate.Validation
{
    internal sealed class FieldSelectionMergingRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new FieldSelectionMergingVisitor(schema);
        }
    }
}
