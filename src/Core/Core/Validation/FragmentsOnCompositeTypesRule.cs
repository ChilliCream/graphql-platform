namespace HotChocolate.Validation
{
    /// <summary>
    /// Fragments can only be declared on unions, interfaces, and objects.
    /// They are invalid on scalars.
    /// They can only be applied on non‐leaf fields.
    /// This rule applies to both inline and named fragments.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Fragments-On-Composite-Types
    /// </summary>
    internal sealed class FragmentsOnCompositeTypesRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new FragmentsOnCompositeTypesVisitor(schema);
        }
    }
}
