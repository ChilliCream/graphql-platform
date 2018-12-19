using System;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// The target field of a field selection must be defined on the scoped
    /// type of the selection set. There are no limitations on alias names.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Field-Selections-on-Objects-Interfaces-and-Unions-Types
    /// </summary>
    internal sealed class FieldMustBeDefinedRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new FieldMustBeDefinedVisitor(schema);
        }
    }
}
