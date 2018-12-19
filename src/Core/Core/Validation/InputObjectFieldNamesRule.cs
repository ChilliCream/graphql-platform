namespace HotChocolate.Validation
{
    /// <summary>
    /// Every input field provided in an input object value must be defined in
    /// the set of possible fields of that input object’s expected type.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Input-Object-Field-Names
    /// </summary>
    internal sealed class InputObjectFieldNamesRule
       : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new InputObjectFieldNamesVisitor(schema);
        }
    }
}
