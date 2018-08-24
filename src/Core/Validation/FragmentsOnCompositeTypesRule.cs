namespace HotChocolate.Validation
{
    internal sealed class FragmentsOnCompositeTypesRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new FragmentsOnCompositeTypesVisitor(schema);
        }
    }
}
