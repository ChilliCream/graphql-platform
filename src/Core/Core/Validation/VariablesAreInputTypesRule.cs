using System.Collections.ObjectModel;

namespace HotChocolate.Validation
{
    /// <summary>
    /// Variables can only be input types. Objects,
    /// unions, and interfaces cannot be used as inputs.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Variables-Are-Input-Types
    /// </summary>
    internal sealed class VariablesAreInputTypesRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new VariablesAreInputTypesVisitor(schema);
        }
    }
}
