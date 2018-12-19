namespace HotChocolate.Validation
{
    /// <summary>
    /// Variable usages must be compatible with the arguments
    /// they are passed to.
    ///
    /// Validation failures occur when variables are used in the context
    /// of types that are complete mismatches, or if a nullable type in a
    ///  variable is passed to a non‐null argument type.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-All-Variable-Usages-are-Allowed
    /// </summary>
    internal sealed class AllVariableUsagesAreAllowedRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new AllVariableUsagesAreAllowedVisitor(schema);
        }
    }
}
