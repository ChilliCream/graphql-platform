using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Configuration.Validation
{
    internal interface ISchemaValidationRule
    {
        IEnumerable<ISchemaError> Validate(
            IReadOnlyList<ITypeSystemObject> typeSystemObjects,
            IReadOnlySchemaOptions options);
    }
}
