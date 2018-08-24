namespace HotChocolate.Validation
{
    internal sealed class FragmentSpreadsMustNotFormCyclesRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new FragmentSpreadsMustNotFormCyclesVisitor(schema);
        }
    }
}
