using System.Linq;

namespace HotChocolate.Validation
{
    /// <summary>
    /// Named fragment spreads must refer to fragments defined within the
    /// document.
    ///
    /// It is a validation error if the target of a spread is not defined.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Fragment-spread-target-defined
    /// </summary>
    internal sealed class FragmentSpreadTargetDefinedRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new FragmentSpreadTargetDefinedVisitor(schema);
        }
    }
}
