using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using HotChocolate.Types;
using static HotChocolate.Configuration.Validation.TypeValidationHelper;

namespace HotChocolate.Configuration.Validation;

public class InputObjectTypeValidationRule : ISchemaValidationRule
{
    public void Validate(
        IReadOnlyList<ITypeSystemObject> typeSystemObjects,
        IReadOnlySchemaOptions options,
        ICollection<ISchemaError> errors)
    {
        if (options.StrictValidation)
        {
            foreach (InputObjectType type in typeSystemObjects.OfType<InputObjectType>())
            {
                EnsureTypeHasFields(type, errors);
                EnsureFieldNamesAreValid(type, errors);
            }
        }
    }

    private static void EnsureOneOfFieldsAreValid(
        InputObjectType type,
        ICollection<ISchemaError> errors)
    {
        if (type.Directives.Contains(WellKnownDirectives.OneOf))
        {
            if (type.Fields.Any(t => t.Type.Kind is TypeKind.NonNull || t.DefaultValue is not null))
            {
                // error 
            }
        }
    }
}
