using System;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// All variables defined by an operation must be used in that operation
    /// or a fragment transitively included by that operation.
    ///
    /// Unused variables cause a validation error.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-All-Variables-Used
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
