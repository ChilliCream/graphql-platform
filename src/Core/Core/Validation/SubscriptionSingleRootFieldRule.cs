using System;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// Subscription operations must have exactly one root field.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Single-root-field
    /// </summary>
    internal sealed class SubscriptionSingleRootFieldRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new SubscriptionSingleRootFieldVisitor(schema);
        }
    }
}
