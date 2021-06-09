using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using static HotChocolate.Configuration.Validation.ComplexOutputTypeValidationHelper;

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
            if (options.StrictValidation)
            {
                foreach (ObjectType type in typeSystemObjects.OfType<ObjectType>())
                {
                    EnsureFieldNamesAreValid(type, errors); // covers #2
                }
            }
        }
    }
}
