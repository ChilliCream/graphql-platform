namespace HotChocolate.Validation
{
    /// <summary>
    /// Input object fields may be required. Much like a field may have
    /// required arguments, an input object may have required fields.
    ///
    /// An input field is required if it has a non‐null type and does not have
    /// a default value. Otherwise, the input object field is optional.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Input-Object-Required-Fields
    /// </summary>
    internal sealed class InputObjectRequiredFieldsRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new InputObjectRequiredFieldsVisitor(schema);
        }
    }
}
