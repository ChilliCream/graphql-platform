namespace HotChocolate.Validation
{
    /// <summary>
    /// The graph of fragment spreads must not form any cycles including
    /// spreading itself. Otherwise an operation could infinitely spread or
    /// infinitely execute on cycles in the underlying data.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Fragment-spreads-must-not-form-cycles
    /// </summary>
    internal sealed class FragmentSpreadsMustNotFormCyclesRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new FragmentSpreadsMustNotFormCyclesVisitor(schema);
        }
    }
}
