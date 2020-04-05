using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using static HotChocolate.Configuration.Validation.ComplexOutputTypeValidationHelper;

#nullable enable

namespace HotChocolate.Configuration.Validation
{
    public class InterfaceTypeValidationRule : ISchemaValidationRule
    {
        public void Validate(
            IReadOnlyList<ITypeSystemObject> typeSystemObjects,
            IReadOnlySchemaOptions options,
            ICollection<ISchemaError> errors)
        {
            if (options.StrictValidation)
            {
                foreach (InterfaceType type in typeSystemObjects.OfType<InterfaceType>())
                {
                    EnsureTypeHasFields(type, errors);
                    EnsureFieldNamesAreValid(type, errors);
                    EnsureInterfacesAreCorrectlyImplemented(type, errors);
                }
            }
        }
    }
}
