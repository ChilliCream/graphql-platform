namespace HotChocolate.Validation
{
    /// <summary>
    /// All variables defined by an operation must be used in that operation
    /// or a fragment transitively included by that operation.
    ///
    /// Unused variables cause a validation error.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-All-Variables-Used
    ///
    /// AND
    ///
    /// Variables are scoped on a per‐operation basis. That means that
    /// any variable used within the context of an operation must be defined
    /// at the top level of that operation
    ///
    /// https://facebook.github.io/graphql/June2018/#sec-All-Variable-Uses-Defined
    /// </summary>
    internal sealed class AllVariablesUsedRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new AllVariablesUsedVisitor(schema);
        }
    }
}
