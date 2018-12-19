using HotChocolate.Types;

namespace HotChocolate.Validation
{
    /// <summary>
    /// GraphQL servers define what directives they support.
    /// For each usage of a directive, the directive must be available
    /// on that server.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Directives-Are-Defined
    /// </summary>
    internal sealed class DirectivesAreDefinedRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new DirectivesAreDefinedVisitor(schema);
        }
    }
}
