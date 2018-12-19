namespace HotChocolate.Validation
{
    /// <summary>
    /// Input objects must not contain more than one field of the same name,
    /// otherwise an ambiguity would exist which includes an ignored portion
    /// of syntax.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Input-Object-Field-Uniqueness
    /// </summary>
    internal sealed class InputObjectFieldUniquenessRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new InputObjectFieldUniquenessVisitor(schema);
        }
    }
}
