using System;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    internal sealed class DirectivesAreInValidLocationsRule
        : QueryVisitorValidationErrorBase
    {
        protected override QueryVisitorErrorBase CreateVisitor(ISchema schema)
        {
            return new DirectivesAreInValidLocationsVisitor(schema);
        }
    }

}
