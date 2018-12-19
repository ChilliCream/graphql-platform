using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Validation
{
    /// <summary>
    /// Field selections on scalars or enums are never allowed,
    /// because they are the leaf nodes of any GraphQL query.
    ///
    /// Conversely the leaf field selections of GraphQL queries
    /// must be of type scalar or enum. Leaf selections on objects,
    /// interfaces, and unions without subfields are disallowed.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Leaf-Field-Selections
    /// </summary>
    internal sealed class LeafFieldSelectionsRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new LeafFieldSelectionsVisitor(schema);
        }
    }
}
