using System;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// Directives are used to describe some metadata or behavioral change on
    /// the definition they apply to.
    ///
    /// When more than one directive of the
    /// same name is used, the expected metadata or behavior becomes ambiguous,
    /// therefore only one of each directive is allowed per location.
    ///
    /// http://facebook.github.io/graphql/draft/#sec-Directives-Are-Unique-Per-Location
    /// </summary>
    internal sealed class DirectivesAreUniquePerLocationRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new DirectivesAreUniquePerLocationVisitor(schema);
        }
    }
}
