using System;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    ///GraphQL servers define what directives they support and where they
    /// support them.
    ///
    /// For each usage of a directive, the directive must be used in a
    /// location that the server has declared support for.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Directives-Are-In-Valid-Locations
    /// </summary>
    internal sealed class DirectivesAreInValidLocationsRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new DirectivesAreInValidLocationsVisitor(schema);
        }
    }

}
