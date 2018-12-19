namespace HotChocolate.Validation
{
    /// <summary>
    /// Literal values must be compatible with the type expected in the position
    /// they are found as per the coercion rules defined in the Type System
    /// chapter.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Values-of-Correct-Type
    /// </summary>
    internal sealed class ValuesOfCorrectTypeRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new ValuesOfCorrectTypeVisitor(schema);
        }
    }
}
