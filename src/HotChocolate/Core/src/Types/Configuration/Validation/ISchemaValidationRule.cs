using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Configuration.Validation
{
    internal interface ISchemaValidationRule
    {
        void Validate(
            IReadOnlyList<ITypeSystemObject> typeSystemObjects,
            IReadOnlySchemaOptions options,
            ICollection<ISchemaError> errors);
    }
}
