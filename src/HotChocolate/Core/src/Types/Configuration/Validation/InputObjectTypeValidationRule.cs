

using System.Collections.Generic;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Configuration.Validation
{
    public class InputObjectTypeValidationRule : ISchemaValidationRule
    {
        public void Validate(
            IReadOnlyList<ITypeSystemObject> typeSystemObjects,
            IReadOnlySchemaOptions options,
            ICollection<ISchemaError> errors)
        {
            throw new System.NotImplementedException();
        }
    }
}
